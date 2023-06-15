using Llama.Collections;
using Llama.Constants;
using Llama.Exceptions;
using Llama.Extensions;
using Llama.Models;
using Llama.Native;
using Llama.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Llama
{
	public class LlamaModel : IDisposable
	{
		private readonly LlamaTokenQueue _evaluationQueue = new();

		private readonly LlamaTokenCollection _inputPrefix;

		private readonly LlamaTokenCollection _inputSuffix;

		private readonly LlamaToken _llama_token_newline;

		private readonly LlamaModelSettings _settings;

		private LlamaTokenCollection _prompt;

		/// <summary>
		/// Please refer `LlamaModelSettings` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
		/// load 20 layers to gpu by default.
		/// </summary>
		/// <param name="llamaModelSettings">The LlamaModel params</param>
		/// <param name="name">Model name</param>
		/// <param name="verbose">Whether to output the detailed info.</param>
		/// <param name="encoding"></param>
		/// <exception cref="RuntimeError"></exception>
		public unsafe LlamaModel(LlamaModelSettings llamaModelSettings, Encoding encoding, string name = "", bool verbose = false)
		{
			if (encoding == System.Text.Encoding.Unicode)
			{
				throw new ArgumentException("Unicode not supported. Did you mean UTF8?");
			}

			this.Name = name;
			this._settings = llamaModelSettings;
			this.Verbose = verbose;
			this.LlamaContext = Utils.Llama_init_from_gpt_params(ref this._settings, encoding);

			this.WithPrompt(this._settings.Prompt);

			// prefix & suffix for instruct mode
			this._inputPrefix = this.LlamaContext.Tokenize("\n\n### Instruction:\n\n", LlamaTokenTags.INPUT, true);
			this._inputSuffix = this.LlamaContext.Tokenize("\n\n### Response:\n\n", LlamaTokenTags.RESPONSE);

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
			this._llama_token_newline = this.LlamaContext.GetToken(13, LlamaTokenTags.UNMANAGED);

			if (this._settings.VerbosePrompt)
			{
				LlamaLogger.Default.Info("\n");
				LlamaLogger.Default.Info($"prompt: '{this._settings.Prompt}'");
				LlamaLogger.Default.Info($"number of tokens in prompt = {this._evaluationQueue.Count}");

				foreach (LlamaToken token in this._evaluationQueue)
				{
					LlamaLogger.Default.Info($"{token.Id} -> '{token}'");
				}

				if (this._settings.KeepContextTokenCount > 0)
				{
					LlamaLogger.Default.Info($"static prompt based on n_keep: '");
					for (int i = 0; i < this._settings.KeepContextTokenCount; i++)
					{
						LlamaLogger.Default.Info(this._evaluationQueue[i].ToString(), true);
						Console.Write(this._evaluationQueue[i].ToString());
					}

					LlamaLogger.Default.Info("\n");
				}

				LlamaLogger.Default.Info("\n");
			}

			foreach (KeyValuePair<int, float> kvp in this._settings.LogitBias)
			{
				string toLog = $"Logit Bias: {Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this.LlamaContext, kvp.Key))} -> {kvp.Value}";
				LlamaLogger.Default.Info(toLog);
			}

			if (this._settings.Interactive && verbose)
			{
				LlamaLogger.Default.Info("interactive mode on.");
			}

			if (verbose)
			{
				LlamaLogger.Default.Info($"sampling: repeat_last_n = {this._settings.RepeatTokenPenaltyWindow}, " +
					$"repeat_penalty = {this._settings.RepeatPenalty}, presence_penalty = {this._settings.PresencePenalty}, " +
					$"frequency_penalty = {this._settings.FrequencyPenalty}, top_k = {this._settings.TopK}, tfs_z = {this._settings.TfsZ}," +
					$" top_p = {this._settings.TopP}, typical_p = {this._settings.TypicalP}, temp = {this._settings.Temp}, mirostat = {this._settings.Mirostat}," +
					$" mirostat_lr = {this._settings.MirostatEta}, mirostat_ent = {this._settings.MirostatTau}");
				LlamaLogger.Default.Info($"generate: n_ctx = {this.LlamaContext.Size}, n_batch = {this._settings.BatchSize}, n_predict = {this._settings.PredictCount}, " +
					$"n_keep = {this._settings.KeepContextTokenCount}");
				LlamaLogger.Default.Info("\n");
			}
		}

		public SafeLlamaContext LlamaContext { get; }

		public string Name { get; set; }

		public bool Verbose { get; set; }

		private readonly AutoResetEvent _inferenceGate = new(true);
		public IEnumerable<LlamaToken> Call(params string[] inputText) => this.Call(inputText.Select(t => new InputText(t)).ToArray());

		private Thread _cleanupThread;

		private void PostResponseProcess()
		{
			IEnumerable<LlamaToken> contextBuffer = this.LlamaContext.Evaluated;

			LlamaTokenCollection postEvaluationTransform = new(this._settings.PostResponseConstextTransformers.Transform(contextBuffer));

			this.LlamaContext.SetBuffer(postEvaluationTransform);

			this.LlamaContext.Evaluate();
		}

		private void Cleanup()
		{
			this._cleanupThread = new Thread(() =>
			{
				this.PostResponseProcess();
				this._inferenceGate.Set();
			});

			this._cleanupThread.Start();
		}

		/// <summary>
		/// Call the model to run inference.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="RuntimeError"></exception>
		public IEnumerable<LlamaToken> Call(params InputText[] text)
		{
			this._inferenceGate.WaitOne();

			LlamaTokenCollection thisCall = new();

			this.ProcessInputText(text);

			bool breakAfterEval = false;

			do
			{
				if (this._evaluationQueue.Count > 0)
				{
					Console.Title = $"{this.LlamaContext.AvailableBuffer}";
					// infinite text generation via context swapping
					// if we run out of context:
					// - take the n_keep first tokens from the original prompt (via n_past)
					// - take half of the last (n_ctx - n_keep) tokens and recompute the logits in batches
					if (this.LlamaContext.AvailableBuffer <= this._evaluationQueue.Count)
					{
						LlamaTokenCollection newContext = this._settings.ContextRoller.GenerateContext(this.LlamaContext, this._evaluationQueue, this._prompt, this._settings.KeepContextTokenCount);

						this.LlamaContext.Clear();
						this.LlamaContext.Write(newContext);
					}
					else
					{
						this.LlamaContext.Write(this._evaluationQueue);
					}
				}

				this._evaluationQueue.Clear();

				this.LlamaContext.Evaluate();

				if(breakAfterEval)
				{
					this.Cleanup();
					yield break;
				}

				if (this._evaluationQueue.Count == 0)
				{
					int top_k = this._settings.TopK <= 0 ? NativeApi.llama_n_vocab(this.LlamaContext) : this._settings.TopK;
					int repeat_last_n = this._settings.RepeatTokenPenaltyWindow < 0 ? this.LlamaContext.Size : this._settings.RepeatTokenPenaltyWindow;

					int id;

					int n_vocab = NativeApi.llama_n_vocab(this.LlamaContext);
					Span<float> logits = Utils.Llama_get_logits(this.LlamaContext, n_vocab);

					// Apply params.logit_bias map
					logits.Add(this._settings.LogitBias);

					LlamaTokenDataArray candidates = new(logits);

					Dictionary<LlamaToken, float> no_penalize = logits.Extract(this.NoPenalize());

					int[] contextIds = this.LlamaContext.Evaluated.Ids.ToArray();

					ulong last_n_repeat = (ulong)LlamaMath.Min(repeat_last_n, this.LlamaContext.Size, contextIds.Length);

					SamplingApi.llama_sample_repetition_penalty(this.LlamaContext, candidates, contextIds, last_n_repeat, this._settings.RepeatPenalty);
					SamplingApi.llama_sample_frequency_and_presence_penalties(this.LlamaContext, candidates, contextIds, last_n_repeat, this._settings.FrequencyPenalty, this._settings.PresencePenalty);

					logits.Update(no_penalize);

					List<LlamaToken> selectedTokens;
                    LlamaTokenCollection selectedCollection;

                    do
                    {
						if (this._settings.Temp <= 0)
						{
							// Greedy sampling
							id = SamplingApi.llama_sample_token_greedy(this.LlamaContext, candidates);
						}
						else
						{
							if (this._settings.Mirostat == 1)
							{
								float mirostat_mu = 2.0f * this._settings.MirostatTau;
								const int MIROSTAT_M = 100;
								SamplingApi.llama_sample_temperature(this.LlamaContext, candidates, this._settings.Temp);
								id = SamplingApi.llama_sample_token_mirostat(this.LlamaContext, candidates, this._settings.MirostatTau, this._settings.MirostatEta, MIROSTAT_M, ref mirostat_mu);
							}
							else if (this._settings.Mirostat == 2)
							{
								float mirostat_mu = 2.0f * this._settings.MirostatTau;
								SamplingApi.llama_sample_temperature(this.LlamaContext, candidates, this._settings.Temp);
								id = SamplingApi.llama_sample_token_mirostat_v2(this.LlamaContext, candidates, this._settings.MirostatTau, this._settings.MirostatEta, ref mirostat_mu);
							}
							else
							{
								do
								{
									// Temperature sampling
									SamplingApi.llama_sample_top_k(this.LlamaContext, candidates, top_k, 1);
									SamplingApi.llama_sample_tail_free(this.LlamaContext, candidates, this._settings.TfsZ, 1);
									SamplingApi.llama_sample_typical(this.LlamaContext, candidates, this._settings.TypicalP, 1);
									SamplingApi.llama_sample_top_p(this.LlamaContext, candidates, this._settings.TopP, 1);
									SamplingApi.llama_sample_temperature(this.LlamaContext, candidates, this._settings.Temp);
									id = SamplingApi.llama_sample_token(this.LlamaContext, candidates);
								} while (thisCall.Count == 0 && id == this._llama_token_newline.Id);
							}
						}

						selectedTokens = this._settings.TokenTransformers.Transform(this._settings, thisCall, this.LlamaContext, this.LlamaContext.GetToken(id, LlamaTokenTags.RESPONSE)).ToList();

                        selectedCollection = new(selectedTokens);
                    } while ((thisCall.IsNullOrWhiteSpace && selectedCollection.IsNullOrWhiteSpace) || selectedCollection.IsNullOrEmpty);

					this._evaluationQueue.Append(selectedCollection);

					thisCall.Append(selectedCollection);

					Console.Write(selectedCollection.ToString());

					foreach (LlamaToken token in selectedCollection)
					{
						yield return token;
					}

					if (this.ShouldBreak(thisCall))
					{
						string call = thisCall.ToString();

						breakAfterEval = true;
					}
				}
			} while (true);
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
		public LlamaTokenCollection Tokenize(string text, string tag = null) => this.LlamaContext.Tokenize(text, tag);

		/// <summary>
		/// Apply a prompt to the model.
		/// </summary>
		/// <param name="prompt"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public LlamaModel WithPrompt(string prompt)
		{
			prompt = prompt.Replace("\r\n", "\n");

			this._settings.Prompt = prompt;

			if (!prompt.StartsWith(" ") && char.IsLetter(prompt[0]))
			{
				LlamaLogger.Default.Warn("Input prompt does not start with space and may have issues as a result");
			}

			this._evaluationQueue.Append(this.LlamaContext.Tokenize(this._settings.Prompt, LlamaTokenTags.PROMPT, true));

			this._prompt = this.LlamaContext.Tokenize(this._settings.Prompt, LlamaTokenTags.PROMPT);

			if (this._evaluationQueue.Count > this.LlamaContext.Size - 4)
			{
				throw new ArgumentException($"prompt is too long ({this._evaluationQueue.Count} tokens, max {this.LlamaContext.Size - 4})");
			}

			// number of tokens to keep when resetting context
			if (this._settings.KeepContextTokenCount < 0 || this._settings.KeepContextTokenCount > this._evaluationQueue.Count || this._settings.Instruct)
			{
				this._settings.KeepContextTokenCount = this._evaluationQueue.Count;
			}

			if (this._evaluationQueue.Count > this.LlamaContext.Size - 4)
			{
				throw new ArgumentException($"prompt is too long ({this._evaluationQueue.Count} tokens, max {this.LlamaContext.Size - 4})");
			}

			return this;
		}

		/// <summary>
		/// Apply the prompt file to the model.
		/// </summary>
		/// <param name="promptFileName"></param>
		/// <returns></returns>
		public LlamaModel WithPromptFile(string promptFileName) => this.WithPrompt(File.ReadAllText(promptFileName));

		private LlamaTokenCollection NoPenalize()
		{
			LlamaTokenCollection collection = new();
			collection.Append(this.LlamaContext.GetToken(13, LlamaTokenTags.UNMANAGED)); //NL
			collection.Append(this.LlamaContext.GetToken(334, LlamaTokenTags.UNMANAGED)); // *
			collection.Append(this.LlamaContext.GetToken(29930, LlamaTokenTags.UNMANAGED)); //*
			return collection;
		}

		private void ProcessInputText(params InputText[] inputTexts)
		{
			foreach (InputText inputText in inputTexts)
			{
				string text = inputText.Content.Replace("\r\n", "\n");

				Console.Write(text);

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
					if (this._settings.Instruct)
					{
						this._evaluationQueue.Append(this._inputPrefix);
					}

					LlamaTokenCollection line_inp = this.LlamaContext.Tokenize(text, inputText.Tag);

					this._evaluationQueue.Append(line_inp);

					// instruct mode: insert response suffix
					if (this._settings.Instruct)
					{
						this._evaluationQueue.Append(this._inputSuffix);
					}
				}
			}
		}

		private bool ShouldBreak(LlamaTokenCollection toTest)
		{
			string returnValue = toTest.ToString();

			foreach (string antiprompt in this._settings.Antiprompt)
			{
				if (returnValue.EndsWith(antiprompt))
				{
					return true;
				}
			}

			if (toTest.Contains(NativeApi.llama_token_eos()))
			{
				return true;
			}

			if (this._settings.PredictCount > 0 && toTest.Count >= this._settings.PredictCount)
			{
				return true;
			}

			return false;
		}
	}
}