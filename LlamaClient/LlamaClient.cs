using Ai.Utils;
using Ai.Utils.Services;
using Llama.Shared;
using Loxifi;
using System.Diagnostics;
using System.Text;

namespace Llama
{
	public class LlamaClient
	{
		private readonly StringBuilder _outBuilder;

		private readonly StringBuilder _queued = new();

		private readonly LlamaSettings _settings;
		private bool _killSent;

		private char _lastChar;
		private RunningProcess _process;

		static LlamaClient()
		{
			ConsoleControl.DisableSelfKill();
		}

		public LlamaClient(LlamaSettings settings)
		{
			this._settings = settings;
			this._outBuilder = new StringBuilder();
			this.Args = GenerateParameters(this._settings);
		}

		public event EventHandler<string>? ResponseReceived;

		public event EventHandler<DisconnectEventArgs> OnDisconnect;

		public event EventHandler? TypingResponse;

		public string Args { get; private set; }

		public bool Connected { get; private set; }

		public bool EndsWithReverse => this._lastChar == this._settings.PrimaryReversePrompt?.Last();

		public bool HasQueuedMessages => this._queued.Length > 0;

		public static string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hashBytes = md5.ComputeHash(inputBytes);

			return Convert.ToHexString(hashBytes); // .NET 5 +
		}

		private bool ShouldKill(string message, string check)
		{
			if (message.Contains('\r') && this.Connected && this._settings.ReturnOnNewLine)
			{
				return true;
			}

			if (check.Length > 10 && check[^10..].Distinct().Count() == 1)
			{
				return true;
			}

			return false;
		}

		Thread _processMonitor;

		public async Task Connect()
		{
			TaskCompletionSource taskCompletionSource = new();

			Console.WriteLine($"{this._settings.MainPath} {this.Args}");

			ProcessSettings processSettings = new(this._settings.MainPath)
			{
				Arguments = Args,
				UnicodeEnvironment = true,
				WorkingDirectory = new FileInfo(this._settings.MainPath).DirectoryName,
				StdOutWrite = (s, o) =>
				{
					this._lastChar = o.Last();

					o = o.Replace("\b", "");

					string check = this._outBuilder.ToString();

					if (string.IsNullOrWhiteSpace(o))
					{
						if (string.IsNullOrWhiteSpace(check))
						{
							return;
						}
					}

					bool shouldKill = this.ShouldKill(o, check);
					//If the message contains a newline and we're set to kill in that event
					if (shouldKill)
					{
						//Kill the child process
						if (!this._killSent)
						{
							//Mark as killed. Sending more than once will exit it
							this._killSent = true;
							Debug.WriteLine("Sending kill signal...");
							ConsoleControl.GenerateKillSignal();
						}
					}

					//If we've detected the reverse or we manually killed, we flush
					if (this.CaptureText(o) || this._killSent)
					{
						if (!this.Connected)
						{
							taskCompletionSource.SetResult();
							this.Connected = true;
						}
						else
						{
							string lastMessage = this._outBuilder.ToString();

							lastMessage = lastMessage.Trim();

							//We may have killed before the turnaround. If so we want
							//the message we pass back to look "normal", as though
							//it was a standard reversal, so we have to append the reverse
							//manually
							if (!lastMessage.EndsWith(this._settings.PrimaryReversePrompt))
							{
								if (!lastMessage.EndsWith(System.Environment.NewLine))
								{
									lastMessage += System.Environment.NewLine;
								}

								lastMessage += this._settings.PrimaryReversePrompt;
							}

							if (!string.IsNullOrEmpty(lastMessage))
							{
								this.ResponseReceived?.Invoke(this, lastMessage);
							}
						}

						_ = this._outBuilder.Clear();
					}
				},
				StdErrWrite = (s, o) =>
				{
					Debug.Write(o);

					ConsoleColor backupColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Write(o);
					Console.ForegroundColor = backupColor;
				}
			};

			this._process = ProcessRunner.StartAsync(processSettings);

			await taskCompletionSource.Task;

			if (!string.IsNullOrWhiteSpace(this._settings.Start))
			{
				string toSend = this._settings.Start;

				if (File.Exists(this._settings.Start))
				{
					toSend = File.ReadAllText(this._settings.Start);
				}

				this.Send(toSend, false);
			}

			_processMonitor = new Thread(async () =>
			{
				await _process;

				DisconnectEventArgs disconnectEventArgs = new()
				{
					ResultCode = await _process.ExitCode
				};

				this.Connected = false;

				string llamaPath = new FileInfo(this._settings.MainPath).DirectoryName;

				string contextDumpPath = Path.Combine(llamaPath, "contexts");

				string potentialDumpPath = Path.Combine(contextDumpPath, $"{disconnectEventArgs.ResultCode}.prompt");
			
				if(File.Exists(potentialDumpPath))
				{
					disconnectEventArgs.RollOverPrompt = potentialDumpPath;
				}

				this.OnDisconnect?.Invoke(this, disconnectEventArgs);
			});

			_processMonitor.Start();
		}

		/// <summary>
		/// Kills the thread and flushes the current output
		/// </summary>
		/// <returns></returns>
		public void Kill()
		{
			ConsoleControl.GenerateKillSignal();
			this._killSent = true;
			string response = this._outBuilder.ToString();
			_ = this._outBuilder.Clear();
			this.ResponseReceived?.Invoke(this, response);
		}

		public void Send(string toSend, bool flush = true)
		{
			//If the last message was a manual kill, then we're not going to have the
			//expected turnaround token, so we need to make sure we append it to the beginning
			//of the next message so that its available in the right place in the context
			if (this._killSent && this._lastChar != this._settings.PrimaryReversePrompt.Last() && !toSend.StartsWith(this._settings.PrimaryReversePrompt.Last()))
			{
				toSend = $"{this._settings.PrimaryReversePrompt}{toSend}";
			}

			toSend = NewlineReplacer.Replace(toSend);

			if (flush)
			{
				_ = this._queued.Append(toSend);

				string concatSend = this._queued.ToString();

				_ = this._queued.Clear();

				this.ConsoleWrite(concatSend);

				this._process.WriteLine(concatSend);
			}
			else
			{
				_ = this._queued.AppendLine(toSend + "\\");
			}

			this._killSent = false;
		}

		private void ConsoleWrite(string toWrite)
		{
			toWrite = toWrite.Replace("\\\r\n", "\r\n").Replace("\\\n", "\n");

			if (toWrite.EndsWith('/'))
			{
				Console.Write(toWrite[..^1]);
			}
			else
			{
				Console.WriteLine(toWrite);
			}
		}
		private static string GenerateParameters(LlamaSettings settings)
		{
			StringBuilder sb = new();

			void AddArgument(string key, string? value = null, bool quoted = false)
			{
				if (value is null)
				{
					_ = sb.Append($" {key}");
				}
				else
				{
					if (!quoted)
					{
						_ = sb.Append($" {key} {value}");
					}
					else
					{
						_ = sb.Append($" {key} \"{value}\"");
					}
				}
			}

			switch (settings.InteractiveMode)
			{
				case InteractiveMode.None:
					break;

				case InteractiveMode.Interactive:
					AddArgument("-i");
					break;

				case InteractiveMode.InteractiveFirst:
					AddArgument("-i");
					AddArgument("--interactive-first");
					break;

				default: throw new NotImplementedException();
			}

			if (settings.UseSessionData)
			{
				string modelPathHash = CreateMD5(settings.ModelPath);
				AddArgument("--prompt-cache", modelPathHash + ".session");
			}

			if (settings.Threads.HasValue)
			{
				AddArgument("-t", $"{settings.Threads.Value}");
			}

			if (settings.NoMemoryMap)
			{
				AddArgument("--no-mmap --mlock");
			}

			if (settings.NoPenalizeNewLine)
			{
				AddArgument("--no-penalize-nl");
			}

			switch (settings.MemoryMode)
			{
				case MemoryMode.Float16:
					AddArgument("--memory_f16");
					break;

				case MemoryMode.Float32:
					AddArgument("--memory_f32");
					break;

				default: throw new NotImplementedException();
			}

			foreach (string r in settings.AllReversePrompts)
			{
				AddArgument("-r", r, true);
			}

			if (!string.IsNullOrEmpty(settings.InSuffix))
			{
				AddArgument("--in-suffix", settings.InSuffix, true);
			}

			if (settings.Temp.HasValue)
			{
				AddArgument("--temp", $"{settings.Temp}");
			}

			if (settings.ContextLength.HasValue)
			{
				AddArgument("-c", $"{settings.ContextLength}");
			}

			if (settings.TokensToPredict.HasValue)
			{
				AddArgument("-n", $"{settings.TokensToPredict}");
			}

			if (settings.RepeatPenalty.HasValue)
			{
				AddArgument("--repeat_penalty", $"{settings.RepeatPenalty}");
			}

			if (settings.Top_P.HasValue)
			{
				AddArgument("--top_p", $"{settings.Top_P}");
			}

			if (settings.RepeatPenaltyWindow.HasValue)
			{
				AddArgument("--repeat_last_n", $"{settings.RepeatPenaltyWindow}");
			}

			if (settings.KeepPromptTokens.HasValue)
			{
				AddArgument("--keep", $"{settings.KeepPromptTokens}");
			}

			if (!string.IsNullOrWhiteSpace(settings.Prompt))
			{
				if (File.Exists(settings.Prompt))
				{
					AddArgument("-f", settings.Prompt, true);
				}
				else
				{
					AddArgument("-p", settings.Prompt);
				}
			}

			if (settings.MiroStat != MiroStatMode.Disabled)
			{
				AddArgument("--mirostat", $"{(int)settings.MiroStat}");
				
				if(settings.MiroStatEntropy.HasValue)
				{
					AddArgument("--mirostat-ent", $"{settings.MiroStatEntropy.Value}");
				}
			}

			if (settings.VerbosePrompt)
			{
				AddArgument("--verbose-prompt");
			}

			if (settings.GpuLayers.HasValue)
			{
				AddArgument("--n-gpu-layers", settings.GpuLayers.ToString());
			}

			foreach (KeyValuePair<int, string> bias in settings.LogitBias)
			{
				AddArgument("-l", $"{bias.Key}{bias.Value}");
			}

			AddArgument("-m", settings.ModelPath, true);

			return sb.ToString();
		}

		private bool CaptureText(string text)
		{
			TypingResponse?.Invoke(this, null);

			_ = this._outBuilder.Append(text);

			Console.Write(text);

			if (!this.Connected)
			{
				return true;
			}
			else
			{
				string o = this._outBuilder.ToString();
				return this._settings.AllReversePrompts.Any(o.EndsWith);
			}
		}
	}
}