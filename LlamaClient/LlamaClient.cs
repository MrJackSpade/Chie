﻿using Llama.Events;
using Llama.Shared;
using Llama.TokenTransformers;
using Llama;
using Llama.Collections;
using Llama.Constants;
using Llama.Events;
using Llama.Models;
using System.Text;

namespace Llama
{
	public class LlamaClient
	{
		private static Thread? _inferenceThread;

		private readonly Encoding _encoding = System.Text.Encoding.UTF8;

		private readonly LlamaModel _model;

		private readonly StringBuilder _outBuilder;

		private readonly List<InputText> _queued = new();

		private readonly LlamaSettings _settings;

		private bool _killSent;

		private char _lastChar;

		public LlamaClient(LlamaSettings settings)
		{
			this._settings = settings;
			this._outBuilder = new StringBuilder();
			this._model = new LlamaModel(this.Params = GenerateParameters(this._settings), this._encoding, verbose: true);
			this._model.LlamaContext.OnContextModification += this.LlamaContext_OnContextModification;
		}

		public event Action<ContextModificationEventArgs> OnContextModification;

		public event EventHandler<DisconnectEventArgs> OnDisconnect;

		public event EventHandler<string>? ResponseReceived;

		public event EventHandler<LlameClientTokenGeneratedEventArgs>? TokenGenerated;

		public bool Connected { get; private set; }

		public bool EndsWithReverse => this._lastChar == this._settings.PrimaryReversePrompt?.Last();

		public bool HasQueuedMessages => this._queued.Count > 0;

		public LlamaModelSettings Params { get; private set; }

		public static string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hashBytes = md5.ComputeHash(inputBytes);

			return Convert.ToHexString(hashBytes); // .NET 5 +
		}

		public async Task Connect()
		{
			if (!string.IsNullOrWhiteSpace(this._settings.Start))
			{			
				string toSend = this._settings.Start;

				if(!this._settings.Prompt.EndsWith("\n"))
				{
					toSend = "\n" + toSend;
				}

				if (File.Exists(this._settings.Start))
				{
					toSend = File.ReadAllText(this._settings.Start);
				}

				this.Send(toSend, LlamaTokenTags.INPUT, false);
			}
		}

		/// <summary>
		/// Kills the thread and flushes the current output
		/// </summary>
		/// <returns></returns>
		public void Kill()
		{
			throw new NotImplementedException();
			this._killSent = true;
			string response = this._outBuilder.ToString();
			_ = this._outBuilder.Clear();
			this.ResponseReceived?.Invoke(this, response);
		}

		public void Send(string toSend, string tag, bool flush = true, bool validateReversal = true)
		{
			//If the last message was a manual kill, then we're not going to have the
			//expected turnaround token, so we need to make sure we append it to the beginning
			//of the next message so that its available in the right place in the context
			if (validateReversal && this._killSent && this._lastChar != this._settings.PrimaryReversePrompt.Last() && !toSend.StartsWith(this._settings.PrimaryReversePrompt.Last()))
			{
				toSend = $"{this._settings.PrimaryReversePrompt}{toSend}";
			}

			if (!flush)
			{
				toSend += "\n";
			}

			this._queued.Add(new InputText(toSend, tag));

			if (flush)
			{
				this.StartInference(this._queued.ToArray());

				this._queued.Clear();
			}

			this._killSent = false;
		}

		private static LlamaModelSettings GenerateParameters(LlamaSettings settings)
		{
			LlamaModelSettings p = new();

			p.TokenTransformers.Add(new NewlineEnsureTransformer());

			switch (settings.InteractiveMode)
			{
				case InteractiveMode.None:
					break;

				case InteractiveMode.Interactive:
					p.Interactive = true;
					break;

				case InteractiveMode.InteractiveFirst:
					p.Interactive = true;
					p.InteractiveFirst = true;
					break;

				default: throw new NotImplementedException();
			}

			if (settings.UseSessionData)
			{
				string modelPathHash = CreateMD5(settings.ModelPath);
				p.SessionPath = modelPathHash + ".session";
			}

			if (settings.Threads.HasValue)
			{
				p.ThreadCount = settings.Threads.Value;
			}

			if (settings.NoMemoryMap)
			{
				p.UseMemoryMap = false;
				p.UseMemoryLock = true;
			}

			if (settings.NoPenalizeNewLine)
			{
				p.PenalizeNewlines = false;
			}

			switch (settings.MemoryMode)
			{
				case MemoryMode.Float16:
					break;

				case MemoryMode.Float32:
					p.MemoryFloat16 = false;
					break;

				default: throw new NotImplementedException();
			}

			p.Antiprompt = settings.AllReversePrompts.ToList();

			if (!string.IsNullOrEmpty(settings.InSuffix))
			{
				p.InputSuffix = settings.InSuffix;
			}

			if (settings.Temp.HasValue)
			{
				p.Temp = settings.Temp.Value;
			}

			if (settings.ContextLength.HasValue)
			{
				p.ContextSize = settings.ContextLength.Value;
			}

			if (settings.TokensToPredict.HasValue)
			{
				p.PredictCount = settings.TokensToPredict.Value;
			}

			if (settings.RepeatPenalty.HasValue)
			{
				p.RepeatPenalty = settings.RepeatPenalty.Value;
			}

			if (settings.Top_P.HasValue)
			{
				p.TopP = settings.Top_P.Value;
			}

			if (settings.RepeatPenaltyWindow.HasValue)
			{
				p.RepeatTokenPenaltyWindow = settings.RepeatPenaltyWindow.Value;
			}

			if (settings.KeepPromptTokens.HasValue)
			{
				p.KeepContextTokenCount = settings.KeepPromptTokens.Value;
			}

			if (!string.IsNullOrWhiteSpace(settings.Prompt))
			{
				if (File.Exists(settings.Prompt))
				{
					p.Prompt = File.ReadAllText(settings.Prompt);
				}
				else
				{
					p.Prompt = settings.Prompt;
				}
			}

			if (settings.MiroStat != MiroStatMode.Disabled)
			{
				p.Mirostat = (int)settings.MiroStat;

				if (settings.MiroStatEntropy.HasValue)
				{
					p.MirostatEta = settings.MiroStatEntropy.Value;
				}
			}

			if (settings.VerbosePrompt)
			{
				p.VerbosePrompt = settings.VerbosePrompt;
			}

			if (settings.GpuLayers.HasValue)
			{
				p.GpuLayerCount = settings.GpuLayers.Value;
			}

			foreach (KeyValuePair<int, string> bias in settings.LogitBias)
			{
				if (string.Equals(bias.Value, "-inf"))
				{
					p.LogitBias!.Add(bias.Key, float.NegativeInfinity);
				}
				else if (string.Equals(bias.Value, "+inf"))
				{
					p.LogitBias!.Add(bias.Key, float.PositiveInfinity);
				}
				else
				{
					p.LogitBias!.Add(bias.Key, float.Parse(bias.Value));
				}
			}

			p.Model = settings.ModelPath;

			return p;
		}

		private void LlamaContext_OnContextModification(ContextModificationEventArgs obj) => this.OnContextModification?.Invoke(obj);

		private void StartInference(params InputText[] data)
		{
			_inferenceThread = new Thread(() =>
			{
				LlamaTokenCollection result = new();

				LlamaToken? lastChunk = null;

				int lastChunkCount = 0;

				foreach (LlamaToken chunk in this._model.Call(data))
				{
					if (string.IsNullOrEmpty(chunk.Value))
					{
						continue;
					}

					this.TokenGenerated?.Invoke(this, new LlameClientTokenGeneratedEventArgs()
					{
						Token = chunk
					});

					result.Append(chunk);
					this._lastChar = chunk.Value[^1];

					if (lastChunk != chunk)
					{
						lastChunkCount = 0;
						lastChunk = chunk;
					}
					else
					{
						lastChunkCount++;
					}

					if (lastChunkCount >= 10)
					{
						break;
					}
				}

				string resultStr = result.ToString();

				if (!resultStr.Contains('\n'))
				{
					Console.Write('\n');
				}

				this.ResponseReceived?.Invoke(this, resultStr);
			});

			_inferenceThread.Start();
		}
	}
}