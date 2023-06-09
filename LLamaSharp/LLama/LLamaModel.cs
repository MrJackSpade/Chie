using LLama.Exceptions;
using LLama.Extensions;
using LLama.Interfaces;
using LLama.Models;
using LLama.Native;
using LLama.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using llama_token_id = System.Int32;

namespace LLama
{
	public class LLamaModel : IChatModel, IDisposable
	{
		private readonly LlamaTokenCollection _evaluationQueue;

		private readonly LlamaTokenCollection _inputPrefix;

		private readonly LlamaTokenCollection _inputSuffix;

		private readonly LlamaToken _llama_token_newline;

		private readonly LlamaTokenCollection _session_tokens;

		private readonly LlamaModelSettings _settings;

		private readonly LlamaTokenCollection _tokenHistory = new();

		private int _contextPointer;

		private bool _input_echo;

		private LlamaTokenCollection _inputBuffer;

		private bool _is_antiprompt;

		private bool _isInteracting;

		private int _n_consumed;

		private int _n_remain;

		private bool _need_to_save_session;

		private string _path_session;

		private LlamaTokenCollection _prompt;

		/// <summary>
		/// Please refer `LlamaModelSettings` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
		/// load 20 layers to gpu by default.
		/// </summary>
		/// <param name="LlamaModelSettings">The LLamaModel params</param>
		/// <param name="name">Model name</param>
		/// <param name="verbose">Whether to output the detailed info.</param>
		/// <param name="encoding"></param>
		/// <exception cref="RuntimeError"></exception>
		public unsafe LLamaModel(LlamaModelSettings LlamaModelSettings, Encoding encoding, string name = "", bool verbose = false)
		{
			if (encoding == System.Text.Encoding.Unicode)
			{
				throw new ArgumentException("Unicode not supported. Did you mean UTF8?");
			}

			this.Name = name;
			this._settings = LlamaModelSettings;
			this.Verbose = verbose;
			this.LlamaContext = Utils.llama_init_from_gpt_params(ref this._settings, encoding);

			// Add a space in front of the first character to match OG llama tokenizer behavior
			this._session_tokens = new LlamaTokenCollection();

			this._path_session = LlamaModelSettings.SessionPath;
			if (!string.IsNullOrEmpty(this._path_session))
			{
				if (verbose)
				{
					LLamaLogger.Default.Info($"Attempting to load saved session from '{this._path_session}'");
				}

				if (!File.Exists(this._path_session))
				{
					LLamaLogger.Default.Warn("Session file does not exist, will create.");
				}

				llama_token_id[] session_tokens = new llama_token_id[LlamaModelSettings.ContextSize];
				ulong n_token_count_out = 0;
				if (!NativeApi.llama_load_session_file(this.LlamaContext, this._path_session, session_tokens, (ulong)LlamaModelSettings.ContextSize, &n_token_count_out))
				{
					throw new RuntimeError($"Failed to load session file {this._path_session}");
				}

				this._session_tokens = this.LlamaContext.Tokenize(session_tokens.Take((int)n_token_count_out));

				if (verbose)
				{
					LLamaLogger.Default.Info($"Loaded a session with prompt size of {this._session_tokens.Count} tokens");
				}
			}

			this.WithPrompt(this._settings.Prompt);

			// prefix & suffix for instruct mode
			this._inputPrefix = this.LlamaContext.Tokenize("\n\n### Instruction:\n\n", true);
			this._inputSuffix = this.LlamaContext.Tokenize("\n\n### Response:\n\n");

			// in instruct mode, we inject a prefix and a suffix to each input by the user
			if (this._settings.Instruct)
			{
				this._settings.InteractiveFirst = true;
				this._settings.Antiprompt.Add("### Instruction:\n\n");
			}

			// enable interactive mode if reverse prompt or interactive start is specified
			if (this._settings.InteractiveFirst)
			{
				this._settings.Interactive = true;
			}

			// determine newline token
			this._llama_token_newline = this.LlamaContext.GetToken(13);

			if (this._settings.VerbosePrompt)
			{
				LLamaLogger.Default.Info("\n");
				LLamaLogger.Default.Info($"prompt: '{this._settings.Prompt}'");
				LLamaLogger.Default.Info($"number of tokens in prompt = {this._inputBuffer.Count}");

				foreach (LlamaToken token in this._inputBuffer)
				{
					LLamaLogger.Default.Info($"{token.Id} -> '{token}'");
				}

				if (this._settings.KeepContextTokenCount > 0)
				{
					LLamaLogger.Default.Info($"static prompt based on n_keep: '");
					for (int i = 0; i < this._settings.KeepContextTokenCount; i++)
					{
						LLamaLogger.Default.Info(this._inputBuffer[i].ToString(), true);
						Console.Write(this._inputBuffer[i].ToString());
					}

					LLamaLogger.Default.Info("\n");
				}

				LLamaLogger.Default.Info("\n");
			}

			foreach (KeyValuePair<int, float> kvp in this._settings.LogitBias)
			{
				string toLog = $"Logit Bias: {Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.LlamaContext, kvp.Key))} -> {kvp.Value}";
				LLamaLogger.Default.Info(toLog);
			}

			if (this._settings.Interactive && verbose)
			{
				LLamaLogger.Default.Info("interactive mode on.");
			}

			if (verbose)
			{
				LLamaLogger.Default.Info($"sampling: repeat_last_n = {this._settings.RepeatTokenPenaltyWindow}, " +
					$"repeat_penalty = {this._settings.RepeatPenalty}, presence_penalty = {this._settings.PresencePenalty}, " +
					$"frequency_penalty = {this._settings.FrequencyPenalty}, top_k = {this._settings.TopK}, tfs_z = {this._settings.TfsZ}," +
					$" top_p = {this._settings.TopP}, typical_p = {this._settings.TypicalP}, temp = {this._settings.Temp}, mirostat = {this._settings.Mirostat}," +
					$" mirostat_lr = {this._settings.MirostatEta}, mirostat_ent = {this._settings.MirostatTau}");
				LLamaLogger.Default.Info($"generate: n_ctx = {this.LlamaContext.Size}, n_batch = {this._settings.BatchSize}, n_predict = {this._settings.PredictCount}, " +
					$"n_keep = {this._settings.KeepContextTokenCount}");
				LLamaLogger.Default.Info("\n");
			}

			this._tokenHistory.AppendControl(Enumerable.Repeat(0, this.LlamaContext.Size));

			if (this._settings.Interactive)
			{
				this._isInteracting = this._settings.InteractiveFirst;
			}

			this._is_antiprompt = false;
			this._input_echo = false;
			this._contextPointer = 0;
			this._n_remain = this._settings.PredictCount;
			this._n_consumed = 0;
			this._evaluationQueue = new();
		}

		public SafeLLamaContext LlamaContext { get; }

		public string Name { get; set; }

		public bool Verbose { get; set; }

		/// <summary>
		/// Call the model to run inference.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="RuntimeError"></exception>
		public IEnumerable<LlamaToken> Call(string text)
		{
			LlamaTokenCollection thisCall = new();

			text = text.Replace("\r\n", "\n");

			this._is_antiprompt = false;

			if (this._contextPointer > 0)
			{
				this._isInteracting = false;
			}

			if (this._isInteracting)
			{
				if (this.Verbose)
				{
					LLamaLogger.Default.Warn("In interacting when calling the model, automatically changed it.", true);
				}

				this._isInteracting = false;
			}

			this.ProcessTextBeforeInfer(text);

			while ((this._n_remain != 0 || this._settings.Interactive) && !this._isInteracting)
			{
				if (this._evaluationQueue.Count > 0)
				{
					Console.Title = $"{this._contextPointer}:{this.LlamaContext.Size}";
					// infinite text generation via context swapping
					// if we run out of context:
					// - take the n_keep first tokens from the original prompt (via n_past)
					// - take half of the last (n_ctx - n_keep) tokens and recompute the logits in batches
					if (this._contextPointer + this._evaluationQueue.Count > this.LlamaContext.Size)
					{
						//We dont care about the embed because its already in the history.
						//Usually it gets evaled seperately but since we're rebuilding
						//its going to be evaled as part of the history rebuild.
						this._evaluationQueue.Clear();

						ContextState contextState = this._settings.ContextRoller.GenerateContext(this.LlamaContext, this._prompt, this._tokenHistory, this._settings.KeepContextTokenCount);

						if(contextState.Tokens.Count > this.LlamaContext.Size)
						{
							throw new ArgumentOutOfRangeException("Generated context state is larger than context size");
						}

						// stop saving session if we run out of context
						this._path_session = string.Empty;

						this._contextPointer = contextState.InsertAt;

						this._evaluationQueue.Append(contextState.Tokens);
					}

					// evaluate tokens in batches
					// embed is typically prepared beforehand to fit within a batch, but not always
					for (int i = 0; i < this._evaluationQueue.Count; i += this._settings.BatchSize)
					{
						int n_eval = this._evaluationQueue.Count - i;

						if (n_eval > this._settings.BatchSize)
						{
							n_eval = this._settings.BatchSize;
						}

						this.LlamaContext.Evaluate(this._evaluationQueue.Skip(i).Take(n_eval), n_eval, this._contextPointer);

						this._contextPointer += n_eval;
					}

					if (this._evaluationQueue.Count > 0 && !string.IsNullOrEmpty(this._path_session))
					{
						this._session_tokens.Append(this._evaluationQueue);
					}
				}

				this._evaluationQueue.Clear();

				if (this._inputBuffer.Count <= this._n_consumed && !this._isInteracting)
				{
					float temp = this._settings.Temp;
					int top_k = this._settings.TopK <= 0 ? NativeApi.llama_n_vocab(this.LlamaContext) : this._settings.TopK;
					float top_p = this._settings.TopP;
					float tfs_z = this._settings.TfsZ;
					float typical_p = this._settings.TypicalP;
					int repeat_last_n = this._settings.RepeatTokenPenaltyWindow < 0 ? this.LlamaContext.Size : this._settings.RepeatTokenPenaltyWindow;
					float repeat_penalty = this._settings.RepeatPenalty;
					float alpha_presence = this._settings.PresencePenalty;
					float alpha_frequency = this._settings.FrequencyPenalty;
					int mirostat = this._settings.Mirostat;
					float mirostat_tau = this._settings.MirostatTau;
					float mirostat_eta = this._settings.MirostatEta;
					bool penalize_nl = this._settings.PenalizeNewlines;

					// optionally save the session on first sample (for faster prompt loading next time)
					if (!string.IsNullOrEmpty(this._path_session) && this._need_to_save_session)
					{
						this._need_to_save_session = false;
						NativeApi.llama_save_session_file(this.LlamaContext, this._path_session, this._session_tokens.Ids.ToArray(), (ulong)this._session_tokens.Count);
					}

					int id;

					int n_vocab = NativeApi.llama_n_vocab(this.LlamaContext);
					Span<float> logits = Utils.llama_get_logits(this.LlamaContext, n_vocab);

					// Apply params.logit_bias map
					logits.Add(this._settings.LogitBias);

					LLamaTokenDataArray candidates = new(logits);

					Dictionary<LlamaToken, float> no_penalize = logits.Extract(this.NoPenalize());

					ulong last_n_repeat = (ulong)Math.Min(Math.Min(this._tokenHistory.Count, repeat_last_n), this.LlamaContext.Size);
					int[] ids = this._tokenHistory.Ids.Skip(this._tokenHistory.Count - (int)last_n_repeat).ToArray();

					SamplingApi.llama_sample_repetition_penalty(this.LlamaContext, candidates, ids, last_n_repeat, repeat_penalty);
					SamplingApi.llama_sample_frequency_and_presence_penalties(this.LlamaContext, candidates, ids, last_n_repeat, alpha_frequency, alpha_presence);

					logits.Update(no_penalize);

					if (temp <= 0)
					{
						// Greedy sampling
						id = SamplingApi.llama_sample_token_greedy(this.LlamaContext, candidates);
					}
					else
					{
						if (mirostat == 1)
						{
							float mirostat_mu = 2.0f * mirostat_tau;
							const int MIROSTAT_M = 100;
							SamplingApi.llama_sample_temperature(this.LlamaContext, candidates, temp);
							id = SamplingApi.llama_sample_token_mirostat(this.LlamaContext, candidates, mirostat_tau, mirostat_eta, MIROSTAT_M, ref mirostat_mu);
						}
						else if (mirostat == 2)
						{
							float mirostat_mu = 2.0f * mirostat_tau;
							SamplingApi.llama_sample_temperature(this.LlamaContext, candidates, temp);
							id = SamplingApi.llama_sample_token_mirostat_v2(this.LlamaContext, candidates, mirostat_tau, mirostat_eta, ref mirostat_mu);
						}
						else
						{
							do
							{
								// Temperature sampling
								SamplingApi.llama_sample_top_k(this.LlamaContext, candidates, top_k, 1);
								SamplingApi.llama_sample_tail_free(this.LlamaContext, candidates, tfs_z, 1);
								SamplingApi.llama_sample_typical(this.LlamaContext, candidates, typical_p, 1);
								SamplingApi.llama_sample_top_p(this.LlamaContext, candidates, top_p, 1);
								SamplingApi.llama_sample_temperature(this.LlamaContext, candidates, temp);
								id = SamplingApi.llama_sample_token(this.LlamaContext, candidates);
							} while (thisCall.Count == 0 && id == this._llama_token_newline.Id);
						}
					}

					List<LlamaToken> selectedTokens = this._settings.TokenTransformers.Transform(this._settings, this.LlamaContext, this.LlamaContext.GetToken(id)).ToList();

					this._tokenHistory.Slide(selectedTokens);

					// add it to the context
					this._evaluationQueue.Append(selectedTokens);

					// add it to this call (session)
					thisCall.Append(selectedTokens);

					// echo this to console
					this._input_echo = true;

					// decrement remaining sampling budget
					this._n_remain--;
				}
				else
				{
					while (this._inputBuffer.Count > this._n_consumed)
					{
						this._evaluationQueue.Append(this._inputBuffer[this._n_consumed]);
						this._tokenHistory.Slide(this._inputBuffer[this._n_consumed]);
						this._n_consumed++;
						if (this._evaluationQueue.Count >= this._settings.BatchSize)
						{
							break;
						}
					}
				}

				if (this._input_echo && !this._isInteracting)
				{
					foreach (LlamaToken token in this._evaluationQueue)
					{
						yield return token;
					}
				}

				if (this._settings.Interactive && this._inputBuffer.Count <= this._n_consumed)
				{
					if (this._settings.Antiprompt.Count > 0)
					{
						string last_output = string.Empty;

						foreach (LlamaToken token in this._tokenHistory)
						{
							last_output += token.Value;
						}

						this._is_antiprompt = false;
						foreach (string antiprompt in this._settings.Antiprompt)
						{
							if (last_output.EndsWith(antiprompt))
							{
								this._isInteracting = true;
								this._is_antiprompt = true;
								break;
							}
						}
					}

					if (this._contextPointer > 0 && this._isInteracting)
					{
						if (this._settings.Instruct)
						{
							foreach (LlamaToken token in this.LlamaContext.Tokenize("\n> "))
							{
								yield return token;
							}
						}

						this._input_echo = false;
						break;
					}

					if (this._evaluationQueue.Count > 0 && this._evaluationQueue.Last().Id == NativeApi.llama_token_eos())
					{
						if (this._settings.Instruct)
						{
							this._isInteracting = true;
						}
						else
						{
							LLamaLogger.Default.Info(" [end of text]");
						}
					}

					if (this._settings.Interactive && this._n_remain <= 0 && this._settings.PredictCount != -1)
					{
						this._n_remain = this._settings.PredictCount;
						this._isInteracting = true;
					}
				}
			}

			if (!string.IsNullOrEmpty(this._path_session) && this._settings.PromptCacheAll)
			{
				LLamaLogger.Default.Info($"saving final output to session file {this._path_session}");
				int[] session_token_array = this._session_tokens.Ids.ToArray();
				NativeApi.llama_save_session_file(this.LlamaContext, this._path_session, session_token_array, (ulong)session_token_array.Length);
			}
		}

		/// <summary>
		/// Chat with the LLaMa model under interactive mode.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="prompt"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public IEnumerable<LlamaToken> Chat(string text, string? prompt = null)
		{
			if (!this._settings.Interactive)
			{
				throw new ArgumentException("The chat API could be only used under interactive model.");
			}

			this._input_echo = false;

			if (!string.IsNullOrEmpty(prompt))
			{
				_ = this.WithPrompt(prompt);
			}

			return this.Call(text);
		}

		public void Dispose() => this.LlamaContext.Dispose();

		public void InitChatAntiprompt(string[] antiprompt) => this._settings.Antiprompt = antiprompt.ToList();

		public void InitChatPrompt(string prompt) => _ = this.WithPrompt(prompt);

		/// <summary>
		/// Load the state from specified path.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="clearPreviousEmbed">Whether to clear previous footprints of this model.</param>
		/// <exception cref="RuntimeError"></exception>
		public void LoadState(string filename, bool clearPreviousEmbed = true)
		{
			byte[] stateMemory = File.ReadAllBytes(filename);
			int stateSize = (int)NativeApi.llama_get_state_size(this.LlamaContext);
			if (stateMemory.Length != stateSize)
			{
				throw new RuntimeError("Failed to validate state size.");
			}

			NativeApi.llama_set_state_data(this.LlamaContext, stateMemory);

			if (clearPreviousEmbed)
			{
				this.WithPrompt(this._settings.Prompt);
			}
		}

		/// <summary>
		/// Save the state to specified path.
		/// </summary>
		/// <param name="filename"></param>
		public void SaveState(string filename)
		{
			ulong stateSize = NativeApi.llama_get_state_size(this.LlamaContext);
			byte[] stateMemory = new byte[stateSize];
			NativeApi.llama_copy_state_data(this.LlamaContext, stateMemory);
			File.WriteAllBytes(filename, stateMemory);
		}

		/// <summary>
		/// Tokenize a string.
		/// </summary>
		/// <param name="text">The utf-8 encoded string to tokenize.</param>
		/// <returns>A list of tokens.</returns>
		/// <exception cref="RuntimeError">If the tokenization failed.</exception>
		public LlamaTokenCollection Tokenize(string text) => this.LlamaContext.Tokenize(text);

		/// <summary>
		/// Apply a prompt to the model.
		/// </summary>
		/// <param name="prompt"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public LLamaModel WithPrompt(string prompt)
		{
			prompt = prompt.Replace("\r\n", "\n");

			this._settings.Prompt = prompt;

			if (!prompt.StartsWith(" ") && char.IsLetter(prompt[0]))
			{
				LLamaLogger.Default.Warn("Input prompt does not start with space and may have issues as a result");
			}

			this._inputBuffer = this.LlamaContext.Tokenize(this._settings.Prompt, true);
			this._prompt = this.LlamaContext.Tokenize(this._settings.Prompt);

			if (this._inputBuffer.Count > this.LlamaContext.Size - 4)
			{
				throw new ArgumentException($"prompt is too long ({this._inputBuffer.Count} tokens, max {this.LlamaContext.Size - 4})");
			}

			ulong n_matching_session_tokens = 0;
			if (this._session_tokens.Count > 0)
			{
				foreach (LlamaToken token in this._session_tokens)
				{
					if (n_matching_session_tokens >= (ulong)this._inputBuffer.Count || token != this._inputBuffer[(int)n_matching_session_tokens])
					{
						break;
					}

					n_matching_session_tokens++;
				}

				if (n_matching_session_tokens >= (ulong)this._inputBuffer.Count)
				{
					LLamaLogger.Default.Info("Session file has exact match for prompt!");
				}
				else if (n_matching_session_tokens < (ulong)(this._inputBuffer.Count / 2))
				{
					LLamaLogger.Default.Warn($"session file has low similarity to prompt ({n_matching_session_tokens} / {this._inputBuffer.Count} tokens); will mostly be reevaluated.");
				}
				else
				{
					LLamaLogger.Default.Info($"Session file matches {n_matching_session_tokens} / {this._inputBuffer.Count} tokens of prompt.");
				}
			}
			// number of tokens to keep when resetting context
			if (this._settings.KeepContextTokenCount < 0 || this._settings.KeepContextTokenCount > this._inputBuffer.Count || this._settings.Instruct)
			{
				this._settings.KeepContextTokenCount = this._inputBuffer.Count;
			}

			if (this._inputBuffer.Count > this.LlamaContext.Size - 4)
			{
				throw new ArgumentException($"prompt is too long ({this._inputBuffer.Count} tokens, max {this.LlamaContext.Size - 4})");
			}

			this._need_to_save_session = !string.IsNullOrEmpty(this._path_session) && n_matching_session_tokens < (ulong)(this._inputBuffer.Count * 3 / 4);

			return this;
		}

		/// <summary>
		/// Apply the prompt file to the model.
		/// </summary>
		/// <param name="promptFileName"></param>
		/// <returns></returns>
		public LLamaModel WithPromptFile(string promptFileName) => this.WithPrompt(File.ReadAllText(promptFileName));

		private LlamaTokenCollection NoPenalize()
		{
			LlamaTokenCollection collection = new();
			collection.Append(this.LlamaContext.GetToken(13)); //NL
			collection.Append(this.LlamaContext.GetToken(334)); // *
			collection.Append(this.LlamaContext.GetToken(29930)); //*
			return collection;
		}

		private void ProcessTextBeforeInfer(string text)
		{
			if (!string.IsNullOrEmpty(this._settings.InputPrefix))
			{
				text = this._settings.InputPrefix + text;
			}

			if (text.Length > 1)
			{
				// append input suffix if any
				if (!string.IsNullOrEmpty(this._settings.InputSuffix))
				{
					text += this._settings.InputSuffix;
				}

				// instruct mode: insert instruction prefix
				if (this._settings.Instruct && !this._is_antiprompt)
				{
					this._n_consumed = this._inputBuffer.Count;
					this._inputBuffer.Append(this._inputPrefix);
				}

				LlamaTokenCollection line_inp = this.LlamaContext.Tokenize(text);
				this._inputBuffer.Append(line_inp);

				// instruct mode: insert response suffix
				if (this._settings.Instruct)
				{
					this._inputBuffer.Append(this._inputSuffix);
				}

				this._n_remain -= line_inp.Count;
			}
		}
	}
}