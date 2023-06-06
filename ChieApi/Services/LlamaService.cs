using Ai.Abstractions;
using Ai.Utils.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Entities;
using Llama;
using Llama.Shared;
using Loxifi;
using System.Text;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
	public class LlamaService
	{
		private readonly ICharacterFactory _characterFactory;
		private readonly SemaphoreSlim _chatLock = new(1);
		private readonly ChatService _chatService;
		private readonly Thread _killThread;

		private readonly LogService _logService;

		private int _characterTimeoutMs = int.MaxValue;

		private LlamaClient _client;

		private DateTime _lastKeyStroke = DateTime.MinValue;

		public LlamaService(ICharacterFactory characterFactory, LogService logService, ChatService chatService)
		{
			this._characterFactory = characterFactory;
			this._logService = logService ?? throw new ArgumentNullException(nameof(logService));
			this._chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

			_ = logService.Log("Constructing Llama Service");

			_ = this.SetState(AiState.Initializing);

			this.Initialization = Task.Run(this.Init);

			this._killThread = new Thread(async () => await this.KillThread());

			this._killThread.Start();
		}

		public AiState AiState { get; private set; }

		public string CharacterName { get; private set; }

		public Task Initialization { get; private set; }

		private string LastChannel { get; set; }

		private long LastMessageId { get; set; }

		public bool CheckIfResponding(string channel) => this.LastChannel == channel && this.AiState == AiState.Responding;

		public ChatEntry[] GetResponses(string channelId, long after) => this._chatService.GetMessages(channelId, after, this.CharacterName);

		public async Task KillThread()
		{
			do
			{
				await Task.Delay(1000);

				if (this._client is null)
				{
					continue;
				}

				try
				{
					if (this.AiState == AiState.Responding && (DateTime.Now - this._lastKeyStroke).TotalMilliseconds > this._characterTimeoutMs)
					{
						_ = this._logService.Log("Timed out. Sending kill signal to return control...");
						this._client.Kill();
						this._client.Send(System.Environment.NewLine, false);
					}
				}
				catch (Exception ex)
				{
					await this._logService.Log(ex.Message);
				}
			} while (true);
		}

		public async Task<long> Send(ChatEntry chatEntry) => await this.Send(new ChatEntry[] { chatEntry });

		private bool TryLock()
		{
			this._chatLock.Wait();

			if (this.AiState != AiState.Idle)
			{
				return false;
			}

			return true;
		}

		private void Unlock() => _ = this._chatLock.Release();

		private CharacterConfiguration _characterConfiguration;

		public async Task<long> Send(ChatEntry[] chatEntries)
		{
			await this.Initialization;

			try
			{
				if (!this.TryLock())
				{
					await this._logService.Log($"Client not idle. Skipping ({chatEntries.Length}) messages.");
					return 0;
				}

				await this._logService.Log("Sending messages to client...");

				LlamaSafeString[] cleanedMessages = chatEntries.Select(LlamaSafeString.Parse).ToArray();

				for (int i = 0; i < chatEntries.Length; i++)
				{
					bool last = i == chatEntries.Length - 1;

					LlamaSafeString cleanedMessage = cleanedMessages[i];

					await this.SendText(chatEntries[i], last);
				}

				await this._logService.Log($"Last Message Id: {this.LastMessageId}");

				return this.LastMessageId;
			}
			finally
			{
				this.Unlock();
			}
		}

		public bool TryGetReply(long originalMessageId, out ChatEntry? chatEntry) => this._chatService.TryGetOriginal(originalMessageId, out chatEntry);

		private async Task Init()
		{
			_ = this._logService.Log("Constructing Llama Client");
			this._characterConfiguration ??= await this._characterFactory.Build();
			this.CharacterName = this._characterConfiguration.CharacterName;
			this._characterTimeoutMs = this._characterConfiguration.Timeout;

			this._client = new LlamaClient(this._characterConfiguration);
			_ = this._logService.Log(System.Text.Json.JsonSerializer.Serialize(this._client.Params, new System.Text.Json.JsonSerializerOptions()
			{
				WriteIndented = true,
				NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
			}));

			this._client.ResponseReceived += new EventHandler<string>(async (s, e) => await this.LlamaClient_ResponseReceived(s, e));
			this._client.TypingResponse += new EventHandler(async (s, e) => await this.LlamaClient_IsTyping(s, e));

			await this._logService.Log("Connecting to client...");
			await this._client.Connect();
			await this._logService.Log("Connected to client.");

			if (this.AiState == AiState.Initializing)
			{
				_ = await this.SetState(AiState.Idle);
			}
		}

		private async Task LlamaClient_IsTyping(object? s, EventArgs e)
		{
			this._lastKeyStroke = DateTime.Now;

			if (this.AiState == AiState.Processing)
			{
				_ = await this.SetState(AiState.Responding);
			}
		}

		private async Task LlamaClient_ResponseReceived(object? sender, string e)
		{
			string? userName = this.CharacterName;
			string? content = e.To("|").Trim();

			if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(content))
			{
				await this._logService.Log("Empty message returned. Ignoring...");
				return;
			}

			if (string.IsNullOrWhiteSpace(userName))
			{
				userName = this.CharacterName;
			}

			ChatEntry chatEntry = new()
			{
				ReplyToId = this.LastMessageId,
				Content = content,
				SourceUser = userName,
				SourceChannel = this.LastChannel
			};

			_ = await this.SetState(AiState.Idle);

			if (chatEntry.Content != null)
			{
				_ = this._chatService.Save(chatEntry);
			}
		}

		private async Task SendText(ChatEntry chatEntry, bool flush)
		{
			await this.Initialization;

			_ = await this.SetState(AiState.Processing);

			this.LastMessageId = await this._chatService.Save(chatEntry);
			this.LastChannel = chatEntry.SourceChannel;

			string toSend;

			if (!string.IsNullOrWhiteSpace(chatEntry.SourceUser))
			{
				if (!this._client.HasQueuedMessages && this._client.EndsWithReverse)
				{
					toSend = $"{chatEntry.SourceUser}> {chatEntry.Content}";
				}
				else
				{
					toSend = $"|{chatEntry.SourceUser}> {chatEntry.Content}";
				}
			}
			else
			{
				toSend = $"[{chatEntry.Content}]";
			}

			this._client.Send(toSend, false);

			if (flush)
			{
				this.ReturnControl(true);
			}
		}

		public bool ReturnControl(bool force, string channelId = null)
		{
			try
			{
				if (force || this.TryLock())
				{
					if (channelId != null && this.LastChannel != channelId)
					{
						return false;
					}

					string toSend;

					if (!this._client.HasQueuedMessages && this._client.EndsWithReverse)
					{
						toSend = $"{this.CharacterName}>";
					}
					else
					{
						toSend = $"|{this.CharacterName}>";
					}

					this._client.Send(toSend, true);
					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				if (force)
				{
					this.Unlock();
				}
			}
		}

		private async Task<bool> SetState(AiState state)
		{
			if (this.AiState != state)
			{
				await this._logService.Log("Setting client state: " + state.ToString());
				this.AiState = state;
				return true;
			}

			return false;
		}
	}
}