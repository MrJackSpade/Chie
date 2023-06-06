using LLama.Exceptions;
using LLama.Extensions;
using LLama.Native;
using LLama.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using llama_token = System.Int32;

namespace LLama
{
	public class LLamaModel : IChatModel, IDisposable
	{
		private readonly List<llama_token> _embed;

		private readonly List<llama_token> _inp_pfx;

		private readonly List<llama_token> _inp_sfx;

		private readonly List<llama_token> _last_n_tokens;

		private readonly List<llama_token> _llama_token_newline;

		private readonly int _n_ctx;

		private readonly LLamaParams _params;

		private List<llama_token> _embed_inp;

		private bool _input_echo;

		private bool _is_antiprompt;

		private bool _is_interacting;

		private int _n_consumed;

		private int _n_past;

		private int _n_remain;

		private int _n_session_consumed;

		private bool _need_to_save_session;

		private string _path_session;

		private List<llama_token> _session_tokens;

		/// <summary>
		/// Please refer `LLamaParams` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
		/// load 20 layers to gpu by default.
		/// </summary>
		/// <param name="llamaParams">The LLamaModel params</param>
		/// <param name="name">Model name</param>
		/// <param name="verbose">Whether to output the detailed info.</param>
		/// <param name="encoding"></param>
		/// <exception cref="RuntimeError"></exception>
		public unsafe LLamaModel(LLamaParams llamaParams, Encoding encoding, string name = "", bool verbose = false)
		{
			this.Name = name;
			this._params = llamaParams;
			this.Verbose = verbose;
			this.NativeHandle = Utils.llama_init_from_gpt_params(ref this._params);

			// Add a space in front of the first character to match OG llama tokenizer behavior
			this._session_tokens = new List<llama_token>();

			this._path_session = llamaParams.SessionPath;
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

				llama_token[] session_tokens = new llama_token[llamaParams.ContextSize];
				ulong n_token_count_out = 0;
				if (!NativeApi.llama_load_session_file(this.NativeHandle, this._path_session, session_tokens, (ulong)llamaParams.ContextSize, &n_token_count_out))
				{
					throw new RuntimeError($"Failed to load session file {this._path_session}");
				}

				this._session_tokens = session_tokens.Take((int)n_token_count_out).ToList();
				if (verbose)
				{
					LLamaLogger.Default.Info($"Loaded a session with prompt size of {this._session_tokens.Count} tokens");
				}
			}

			this._n_ctx = NativeApi.llama_n_ctx(this.NativeHandle);

			this.WithPrompt(this._params.Prompt, encoding);

			// prefix & suffix for instruct mode
			this._inp_pfx = Utils.llama_tokenize(this.NativeHandle, "\n\n### Instruction:\n\n", true, encoding);
			this._inp_sfx = Utils.llama_tokenize(this.NativeHandle, "\n\n### Response:\n\n", false, encoding);

			// in instruct mode, we inject a prefix and a suffix to each input by the user
			if (this._params.Instruct)
			{
				this._params.InteractiveFirst = true;
				this._params.Antiprompt.Add("### Instruction:\n\n");
			}

			// enable interactive mode if reverse prompt or interactive start is specified
			if (this._params.InteractiveFirst)
			{
				this._params.Interactive = true;
			}

			// determine newline token
			this._llama_token_newline = Utils.llama_tokenize(this.NativeHandle, "\n", false, encoding);

			if (this._params.VerbosePrompt)
			{
				LLamaLogger.Default.Info("\n");
				LLamaLogger.Default.Info($"prompt: '{this._params.Prompt}'");
				LLamaLogger.Default.Info($"number of tokens in prompt = {this._embed_inp.Count}");
				for (int i = 0; i < this._embed_inp.Count; i++)
				{
					LLamaLogger.Default.Info($"{this._embed_inp[i]} -> '{Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.NativeHandle, this._embed_inp[i]))}'");
				}

				if (this._params.KeepContextTokenCount > 0)
				{
					LLamaLogger.Default.Info($"static prompt based on n_keep: '");
					for (int i = 0; i < this._params.KeepContextTokenCount; i++)
					{
						string part = Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.NativeHandle, this._embed_inp[i]));
						LLamaLogger.Default.Info($"{part}", true);
						Console.Write(part);
					}

					LLamaLogger.Default.Info("\n");
				}

				LLamaLogger.Default.Info("\n");
			}

			foreach (KeyValuePair<int, float> kvp in this._params.LogitBias)
			{
				string toLog = $"Logit Bias: {Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.NativeHandle, kvp.Key))} -> {kvp.Value}";
				LLamaLogger.Default.Info(toLog);
			}

			if (this._params.Interactive && verbose)
			{
				LLamaLogger.Default.Info("interactive mode on.");
			}

			if (verbose)
			{
				LLamaLogger.Default.Info($"sampling: repeat_last_n = {this._params.RepeatTokenPenaltyWindow}, " +
					$"repeat_penalty = {this._params.RepeatPenalty}, presence_penalty = {this._params.PresencePenalty}, " +
					$"frequency_penalty = {this._params.FrequencyPenalty}, top_k = {this._params.TopK}, tfs_z = {this._params.TfsZ}," +
					$" top_p = {this._params.TopP}, typical_p = {this._params.TypicalP}, temp = {this._params.Temp}, mirostat = {this._params.Mirostat}," +
					$" mirostat_lr = {this._params.MirostatEta}, mirostat_ent = {this._params.MirostatTau}");
				LLamaLogger.Default.Info($"generate: n_ctx = {this._n_ctx}, n_batch = {this._params.BatchSize}, n_predict = {this._params.PredictCount}, " +
					$"n_keep = {this._params.KeepContextTokenCount}");
				LLamaLogger.Default.Info("\n");
			}

			this._last_n_tokens = Enumerable.Repeat(0, this._n_ctx).ToList();

			if (this._params.Interactive)
			{
				if (verbose)
				{
					LLamaLogger.Default.Info("== Running in interactive mode. ==");
				}

				this._is_interacting = this._params.InteractiveFirst;
			}

			this._is_antiprompt = false;
			this._input_echo = false;
			this._n_past = 0;
			this._n_remain = this._params.PredictCount;
			this._n_consumed = 0;
			this._n_session_consumed = 0;
			this._embed = new List<llama_token>();
		}

		public string Name { get; set; }

		public SafeLLamaContextHandle NativeHandle { get; }

		public bool Verbose { get; set; }

		/// <summary>
		/// Call the model to run inference.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="RuntimeError"></exception>
		public IEnumerable<string> Call(string text, Encoding encoding)
		{
			this._is_antiprompt = false;

			if (this._n_past > 0)
			{
				this._is_interacting = false;
			}

			if (this._is_interacting)
			{
				if (this.Verbose)
				{
					LLamaLogger.Default.Warn("In interacting when calling the model, automatically changed it.", true);
				}

				this._is_interacting = false;
			}

			this.ProcessTextBeforeInfer(text, encoding);

			while ((this._n_remain != 0 || this._params.Interactive) && !this._is_interacting)
			{
				if (this._embed.Count > 0)
				{
					// infinite text generation via context swapping
					// if we run out of context:
					// - take the n_keep first tokens from the original prompt (via n_past)
					// - take half of the last (n_ctx - n_keep) tokens and recompute the logits in batches
					if (this._n_past + this._embed.Count > this._n_ctx)
					{
						int n_left = this._n_past - this._params.KeepContextTokenCount;

						this._n_past = Math.Max(1, this._params.KeepContextTokenCount);

						// insert n_left/2 tokens at the start of embed from last_n_tokens
						this._embed.InsertRange(0, this._last_n_tokens.Take(this._last_n_tokens.Count - this._embed.Count).Skip(this._n_ctx - n_left / 2 - this._embed.Count));

						// stop saving session if we run out of context
						this._path_session = string.Empty;
					}

					// try to reuse a matching prefix from the loaded session instead of re-eval (via n_past)
					// REVIEW
					if (this._n_session_consumed < this._session_tokens.Count)
					{
						int i = 0;
						for (; i < this._embed.Count; i++)
						{
							if (this._embed[i] != this._session_tokens[this._n_session_consumed])
							{
								this._session_tokens = this._session_tokens.Take(this._n_session_consumed).ToList();
								break;
							}

							this._n_past++;
							this._n_session_consumed++;

							if (this._n_session_consumed >= this._session_tokens.Count)
							{
								i++;
								break;
							}
						}

						if (i > 0)
						{
							this._embed.RemoveRange(0, i);
						}
					}

					// evaluate tokens in batches
					// embed is typically prepared beforehand to fit within a batch, but not always
					for (int i = 0; i < this._embed.Count; i += this._params.BatchSize)
					{
						int n_eval = this._embed.Count - i;

						if (n_eval > this._params.BatchSize)
						{
							n_eval = this._params.BatchSize;
						}

						int[] array = this._embed.Skip(i).ToArray();
						if (NativeApi.llama_eval(this.NativeHandle, array, n_eval, this._n_past, this._params.ThreadCount) != 0)
						{
							LLamaLogger.Default.Error($"Failed to eval.");
							throw new RuntimeError("Failed to eval.");
						}

						this._n_past += n_eval;
					}

					if (this._embed.Count > 0 && !string.IsNullOrEmpty(this._path_session))
					{
						this._session_tokens.AddRange(this._embed);
						this._n_session_consumed = this._session_tokens.Count;
					}
				}

				this._embed.Clear();

				if (this._embed_inp.Count <= this._n_consumed && !this._is_interacting)
				{
					float temp = this._params.Temp;
					int top_k = this._params.TopK <= 0 ? NativeApi.llama_n_vocab(this.NativeHandle) : this._params.TopK;
					float top_p = this._params.TopP;
					float tfs_z = this._params.TfsZ;
					float typical_p = this._params.TypicalP;
					int repeat_last_n = this._params.RepeatTokenPenaltyWindow < 0 ? this._n_ctx : this._params.RepeatTokenPenaltyWindow;
					float repeat_penalty = this._params.RepeatPenalty;
					float alpha_presence = this._params.PresencePenalty;
					float alpha_frequency = this._params.FrequencyPenalty;
					int mirostat = this._params.Mirostat;
					float mirostat_tau = this._params.MirostatTau;
					float mirostat_eta = this._params.MirostatEta;
					bool penalize_nl = this._params.PenalizeNewlines;

					// optionally save the session on first sample (for faster prompt loading next time)
					if (!string.IsNullOrEmpty(this._path_session) && this._need_to_save_session)
					{
						this._need_to_save_session = false;
						NativeApi.llama_save_session_file(this.NativeHandle, this._path_session, this._session_tokens.ToArray(), (ulong)this._session_tokens.Count);
					}

					int id;

					int n_vocab = NativeApi.llama_n_vocab(this.NativeHandle);
					Span<float> logits = Utils.llama_get_logits(this.NativeHandle, n_vocab);

					// Apply params.logit_bias map
					foreach ((int key, float value) in this._params.LogitBias)
					{
						logits[key] += value;
					}

					List<LLamaTokenData> candidates = new(n_vocab);

					for (llama_token token_id = 0; token_id < n_vocab; token_id++)
					{
						candidates.Add(new LLamaTokenData(token_id, logits[token_id], 0.0f));
					}

					LLamaTokenDataArray candidates_p = new(candidates.ToArray(), (ulong)candidates.Count, false);

					// Apply penalties
					float nl_logit = logits[NativeApi.llama_token_nl()];
					int last_n_repeat = Math.Min(Math.Min(this._last_n_tokens.Count, repeat_last_n), this._n_ctx);
					SamplingApi.llama_sample_repetition_penalty(this.NativeHandle, candidates_p,
						this._last_n_tokens.Skip(this._last_n_tokens.Count - last_n_repeat).ToArray(),
						(ulong)last_n_repeat, repeat_penalty);
					SamplingApi.llama_sample_frequency_and_presence_penalties(this.NativeHandle, candidates_p,
						this._last_n_tokens.Skip(this._last_n_tokens.Count - last_n_repeat).ToArray(),
						(ulong)last_n_repeat, alpha_frequency, alpha_presence);
					if (!penalize_nl)
					{
						logits[NativeApi.llama_token_nl()] = nl_logit;
					}

					if (temp <= 0)
					{
						// Greedy sampling
						id = SamplingApi.llama_sample_token_greedy(this.NativeHandle, candidates_p);
					}
					else
					{
						if (mirostat == 1)
						{
							float mirostat_mu = 2.0f * mirostat_tau;
							const int MIROSTAT_M = 100;
							SamplingApi.llama_sample_temperature(this.NativeHandle, candidates_p, temp);
							id = SamplingApi.llama_sample_token_mirostat(this.NativeHandle, candidates_p, mirostat_tau, mirostat_eta, MIROSTAT_M, ref mirostat_mu);
						}
						else if (mirostat == 2)
						{
							float mirostat_mu = 2.0f * mirostat_tau;
							SamplingApi.llama_sample_temperature(this.NativeHandle, candidates_p, temp);
							id = SamplingApi.llama_sample_token_mirostat_v2(this.NativeHandle, candidates_p, mirostat_tau, mirostat_eta, ref mirostat_mu);
						}
						else
						{
							// Temperature sampling
							SamplingApi.llama_sample_top_k(this.NativeHandle, candidates_p, top_k, 1);
							SamplingApi.llama_sample_tail_free(this.NativeHandle, candidates_p, tfs_z, 1);
							SamplingApi.llama_sample_typical(this.NativeHandle, candidates_p, typical_p, 1);
							SamplingApi.llama_sample_top_p(this.NativeHandle, candidates_p, top_p, 1);
							SamplingApi.llama_sample_temperature(this.NativeHandle, candidates_p, temp);
							id = SamplingApi.llama_sample_token(this.NativeHandle, candidates_p);
						}
					}

					this._last_n_tokens.RemoveAt(0);
					this._last_n_tokens.Add(id);

					// replace end of text token with newline token when in interactive mode
					if (id == NativeApi.llama_token_eos() && this._params.Interactive && !this._params.Instruct)
					{
						id = this._llama_token_newline[0];
						if (this._params.Antiprompt.Count != 0)
						{
							// tokenize and inject first reverse prompt
							List<int> first_antiprompt = Utils.llama_tokenize(this.NativeHandle, this._params.Antiprompt[0], false, encoding);
							this._embed_inp.AddRange(first_antiprompt);
						}
					}

					// add it to the context
					this._embed.Add(id);

					// echo this to console
					this._input_echo = true;

					// decrement remaining sampling budget
					this._n_remain--;
				}
				else
				{
					while (this._embed_inp.Count > this._n_consumed)
					{
						this._embed.Add(this._embed_inp[this._n_consumed]);
						this._last_n_tokens.RemoveAt(0);
						this._last_n_tokens.Add(this._embed_inp[this._n_consumed]);
						this._n_consumed++;
						if (this._embed.Count >= this._params.BatchSize)
						{
							break;
						}
					}
				}

				if (this._input_echo && !this._is_interacting)
				{
					foreach (int id in this._embed)
					{
						string res = Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.NativeHandle, id));
						yield return res;
					}
				}

				if (this._params.Interactive && this._embed_inp.Count <= this._n_consumed)
				{
					if (this._params.Antiprompt.Count > 0)
					{
						string last_output = string.Empty;
						foreach (int id in this._last_n_tokens)
						{
							last_output += Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.NativeHandle, id));
						}

						this._is_antiprompt = false;
						foreach (string antiprompt in this._params.Antiprompt)
						{
							if (last_output.EndsWith(antiprompt))
							{
								this._is_interacting = true;
								this._is_antiprompt = true;
								break;
							}
						}
					}

					if (this._n_past > 0 && this._is_interacting)
					{
						if (this._params.Instruct)
						{
							yield return "\n> ";
						}

						this._input_echo = false;
						break;
					}

					if (this._embed.Count > 0 && this._embed.Last() == NativeApi.llama_token_eos())
					{
						if (this._params.Instruct)
						{
							this._is_interacting = true;
						}
						else
						{
							LLamaLogger.Default.Info(" [end of text]");
						}
					}

					if (this._params.Interactive && this._n_remain <= 0 && this._params.PredictCount != -1)
					{
						this._n_remain = this._params.PredictCount;
						this._is_interacting = true;
					}
				}
			}

			if (!string.IsNullOrEmpty(this._path_session) && this._params.PromptCacheAll)
			{
				LLamaLogger.Default.Info($"saving final output to session file {this._path_session}");
				int[] session_token_array = this._session_tokens.ToArray();
				NativeApi.llama_save_session_file(this.NativeHandle, this._path_session, session_token_array, (ulong)session_token_array.Length);
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
		public IEnumerable<string> Chat(string text, Encoding encoding, string? prompt = null)
		{
			if (!this._params.Interactive)
			{
				throw new ArgumentException("The chat API could be only used under interactive model.");
			}

			this._input_echo = false;
			if (!string.IsNullOrEmpty(prompt))
			{
				_ = this.WithPrompt(prompt, encoding);
			}

			return this.Call(text, encoding);
		}

		/// <summary>
		/// Detokenize a list of tokens.
		/// </summary>
		/// <param name="tokens">The list of tokens to detokenize.</param>
		/// <returns>The detokenized string.</returns>
		public string DeTokenize(IEnumerable<llama_token> tokens)
		{
			Debug.Assert(this.NativeHandle.DangerousGetHandle() != IntPtr.Zero);
			string output = string.Empty;
			foreach (int token in tokens)
			{
				output += Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.NativeHandle, token));
			}

			return output;
		}

		public void Dispose() => this.NativeHandle.Dispose();

		public void InitChatAntiprompt(string[] antiprompt) => this._params.Antiprompt = antiprompt.ToList();

		public void InitChatPrompt(string prompt, Encoding encoding) => _ = this.WithPrompt(prompt, encoding);

		/// <summary>
		/// Load the state from specified path.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="clearPreviousEmbed">Whether to clear previous footprints of this model.</param>
		/// <exception cref="RuntimeError"></exception>
		public void LoadState(string filename, Encoding encoding, bool clearPreviousEmbed = true)
		{
			byte[] stateMemory = File.ReadAllBytes(filename);
			int stateSize = (int)NativeApi.llama_get_state_size(this.NativeHandle);
			if (stateMemory.Length != stateSize)
			{
				throw new RuntimeError("Failed to validate state size.");
			}

			NativeApi.llama_set_state_data(this.NativeHandle, stateMemory);

			if (clearPreviousEmbed)
			{
				this.WithPrompt(this._params.Prompt, encoding);
			}
		}

		/// <summary>
		/// Save the state to specified path.
		/// </summary>
		/// <param name="filename"></param>
		public void SaveState(string filename)
		{
			ulong stateSize = NativeApi.llama_get_state_size(this.NativeHandle);
			byte[] stateMemory = new byte[stateSize];
			NativeApi.llama_copy_state_data(this.NativeHandle, stateMemory);
			File.WriteAllBytes(filename, stateMemory);
		}

		/// <summary>
		/// Tokenize a string.
		/// </summary>
		/// <param name="text">The utf-8 encoded string to tokenize.</param>
		/// <returns>A list of tokens.</returns>
		/// <exception cref="RuntimeError">If the tokenization failed.</exception>
		public List<llama_token> Tokenize(string text, Encoding encoding)
		{
			Debug.Assert(this.NativeHandle.DangerousGetHandle() != IntPtr.Zero);
			int n_ctx = NativeApi.llama_n_ctx(this.NativeHandle);
			int[] tokens = new llama_token[n_ctx];
			int n_tokens = NativeApi.llama_tokenize(this.NativeHandle, text, encoding, tokens, n_ctx, true);
			if (n_tokens < 0)
			{
				throw new RuntimeError($"Failed to tokenize: text=\"{text}\" n_tokens={n_tokens}");
			}

			return tokens.Take(n_tokens).ToList();
		}

		/// <summary>
		/// Apply a prompt to the model.
		/// </summary>
		/// <param name="prompt"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public LLamaModel WithPrompt(string prompt, Encoding encoding)
		{
			this._params.Prompt = prompt.Insert(0, " ");
			this._embed_inp = Utils.llama_tokenize(this.NativeHandle, this._params.Prompt, true, encoding);

			if (this._embed_inp.Count > this._n_ctx - 4)
			{
				throw new ArgumentException($"prompt is too long ({this._embed_inp.Count} tokens, max {this._n_ctx - 4})");
			}

			ulong n_matching_session_tokens = 0;
			if (this._session_tokens.Count > 0)
			{
				foreach (int id in this._session_tokens)
				{
					if (n_matching_session_tokens >= (ulong)this._embed_inp.Count || id != this._embed_inp[(int)n_matching_session_tokens])
					{
						break;
					}

					n_matching_session_tokens++;
				}

				if (n_matching_session_tokens >= (ulong)this._embed_inp.Count)
				{
					LLamaLogger.Default.Info("Session file has exact match for prompt!");
				}
				else if (n_matching_session_tokens < (ulong)(this._embed_inp.Count / 2))
				{
					LLamaLogger.Default.Warn($"session file has low similarity to prompt ({n_matching_session_tokens} " +
						$"/ {this._embed_inp.Count} tokens); will mostly be reevaluated.");
				}
				else
				{
					LLamaLogger.Default.Info($"Session file matches {n_matching_session_tokens} / {this._embed_inp.Count} " +
						$"tokens of prompt.");
				}
			}
			// number of tokens to keep when resetting context
			if (this._params.KeepContextTokenCount < 0 || this._params.KeepContextTokenCount > this._embed_inp.Count || this._params.Instruct)
			{
				this._params.KeepContextTokenCount = this._embed_inp.Count;
			}

			if (this._embed_inp.Count > this._n_ctx - 4)
			{
				throw new ArgumentException($"prompt is too long ({this._embed_inp.Count} tokens, max {this._n_ctx - 4})");
			}

			this._need_to_save_session = !string.IsNullOrEmpty(this._path_session) && n_matching_session_tokens < (ulong)(this._embed_inp.Count * 3 / 4);

			return this;
		}

		/// <summary>
		/// Apply the prompt file to the model.
		/// </summary>
		/// <param name="promptFileName"></param>
		/// <returns></returns>
		public LLamaModel WithPromptFile(string promptFileName, Encoding encoding) => this.WithPrompt(File.ReadAllText(promptFileName), encoding);

		private void ProcessTextBeforeInfer(string text, Encoding encoding)
		{
			if (!string.IsNullOrEmpty(this._params.InputPrefix))
			{
				text = this._params.InputPrefix + text;
			}

			if (text.Length > 1)
			{
				// append input suffix if any
				if (!string.IsNullOrEmpty(this._params.InputSuffix))
				{
					text += this._params.InputSuffix;
				}

				// instruct mode: insert instruction prefix
				if (this._params.Instruct && !this._is_antiprompt)
				{
					this._n_consumed = this._embed_inp.Count;
					this._embed_inp.AddRange(this._inp_pfx);
				}

				List<int> line_inp = Utils.llama_tokenize(this.NativeHandle, text, false, encoding);
				this._embed_inp.AddRange(line_inp);

				// instruct mode: insert response suffix
				if (this._params.Instruct)
				{
					this._embed_inp.AddRange(this._inp_sfx);
				}

				this._n_remain -= line_inp.Count;
			}
		}
	}
}