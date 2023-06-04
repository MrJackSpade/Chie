using Ai.Utils;
using Ai.Utils.Extensions;
using Chie;
using ChieApi.Client;
using ChieApi.Shared.Entities;
using Discord;
using Discord.WebSocket;
using DiscordGpt.Constants;
using DiscordGpt.Extensions;
using DiscordGpt.Services;
using System.Text.RegularExpressions;

namespace DiscordGpt
{
	internal class DiscordIntegrationService
	{
		private readonly ActiveChannelCollection _activeChannels;

		private readonly ChieClient _chieClient = new();
		private readonly ChieMessageService _chieMessageService;
		private readonly DiscordClient _discordClient;
		private readonly Dictionary<ulong, Dictionary<string, string>> _guildEmotes = new();
		private readonly Logger _logger;
		private readonly NameService _nameService;
		private readonly DiscordIntegrationSettings _settings;
		private readonly StartInfo _startInfo;
		private Task? _receiveTask;
		private Task? _typingTask;

		public DiscordIntegrationService(ActiveChannelCollection activeChannelCollection, NameService nameService, ChieMessageService messageService, StartInfo startInfo, DiscordClient discordClient, Logger logger, DiscordIntegrationSettings settings)
		{
			this._activeChannels = activeChannelCollection;
			this._nameService = nameService;
			this._chieMessageService = messageService;
			this._startInfo = startInfo;
			this._settings = settings;
			this._logger = logger;
			this._discordClient = discordClient;
			this._discordClient.OnReactionAdded += this.OnReactionAdded;
			this._discordClient.OnTypingStart += this.OnTypingStart;
		}

		private async Task OnTypingStart(Cacheable<IUser, ulong> cachedUser, Cacheable<IMessageChannel, ulong> cachedChannel)
		{
			if (!this._activeChannels.TryGetValue(cachedChannel.Id, out ActiveChannel? activeChannel))
			{
				return;
			}

			_chieMessageService.TryDelaySend(DateTime.Now.AddSeconds(10));
		}

		public async Task ProcessIncomingMessage(ChatEntry messageResponse)
		{
			await this._logger.Write("Response Received. Cleaning...");

			string cleanedMessage = messageResponse.Content;

			ActiveChannel activeChannel = this._activeChannels[messageResponse.SourceChannel];

			if (activeChannel is null)
			{
				await this._logger.Write($"Active channel not found for message {messageResponse.Id}");
				return;
			}

			if (_settings.UseServerEmotes && activeChannel.Channel is SocketTextChannel stcb)
			{
				cleanedMessage = this.EmojiFill(stcb, cleanedMessage);
			}

			cleanedMessage = cleanedMessage.DiscordEscape();

			await this._logger.Write("Sending to chat...");

			await this._logger.Write($"Message: {cleanedMessage}", LogLevel.Private);

			_ = await activeChannel.Channel.SendMessageAsync(cleanedMessage);
		}

		public async Task Start()
		{
			Console.WriteLine("Connecting Discord...");
			await this._discordClient.Connect();
			Console.WriteLine("Connected Discord.");

			this._discordClient.OnMessageReceived += this.Client_OnMessageReceived;

			this._typingTask = Task.Run(this.TypingLoop);
			this._receiveTask = Task.Run(this.ReceiveLoop);

			await LoopUtil.Forever();
		}

		private async Task Client_OnMessageReceived(SocketMessage arg)
		{
			if (arg.Channel.Id == Logger.DEBUG_CHANNEL_ID)
			{
				return;
			}

			await this._logger.Write($"Received Message on Channel [{arg.Channel.Id}]");

			if (!arg.IsVisible())
			{
				await this._logger.Write("Message not visible. Marking.");
				await this._chieMessageService.MarkUnseen(arg);
				return;
			}

			if (arg.Channel is SocketDMChannel && !this._settings.AllowDms)
			{
				await this._logger.Write("Channel is DM but DM's are disabled");
				await this._chieMessageService.MarkUnseen(arg);
				return;
			}

			if (arg.Author.Username == this._discordClient.CurrentUser.Username)
			{
				await this._logger.Write("Self Message. Skipping.");
				return;
			}

			if (!this._settings.PublicChannels.Contains(arg.Channel.Id) && arg.Channel is not SocketDMChannel)
			{
				await this._logger.Write("Message not on visible channel. Skipping");
				return;
			}

			_ = Task.Run(async () => await _chieMessageService.DeferredMessageProcessing(arg));
		}

		private string EmojiFill(SocketTextChannel channel, string message)
		{
			SocketGuild guild = channel.Guild;

			if (!this._guildEmotes.TryGetValue(guild.Id, out Dictionary<string, string>? emotes))
			{
				emotes = guild.Emotes.ToDictionary(e => $"\\*+[a-zA-Z\\s]*{e.Name}[a-zA-Z\\s]*\\*+", e => $"<:{e.Name}:{e.Id}>");

				this._guildEmotes.Add(guild.Id, emotes);
			}

			string newMessage = message;

			foreach (KeyValuePair<string, string> kvp in emotes)
			{
				newMessage = Regex.Replace(newMessage, kvp.Key, kvp.Value, RegexOptions.IgnoreCase);
			}

			return newMessage;
		}

		private async Task OnReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> cachedMessage, Discord.Cacheable<Discord.IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
		{
			if(reaction.Emote.Name != Emoji.Parse(Emojis.GO).Name)
			{
				return;
			}

			if (!this._activeChannels.TryGetValue(cachedChannel.Id, out ActiveChannel? activeChannel))
			{
				return;
			}

			IUserMessage reactedMessage = await cachedMessage.DownloadAsync();

			if (reactedMessage.Author.Username != _discordClient.CurrentUser.Username)
			{
				return;
			}

			await _chieClient.ContinueRequest(activeChannel.ChieName);
		}

		private async Task ReceiveLoop()
		{
			await LoopUtil.Loop(async () =>
			{
				foreach (ActiveChannel activeChannel in this._activeChannels)
				{
					ChatEntry[] receivedMessages = (await this._chieClient.GetResponses(activeChannel.ChieName, activeChannel.LastMessageId)).ToArray();

					foreach (ChatEntry chatEntry in receivedMessages.OrderBy(r => r.Id))
					{
						activeChannel.LastMessageId = chatEntry.Id;

						if (chatEntry.DateCreated < this._startInfo.StartTime)
						{
							continue;
						}

						await this.ProcessIncomingMessage(chatEntry);
					}
				}
			}, 1000, async ex => await this._logger.Write(ex));
		}

		private async Task TypingLoop()
		{
			await LoopUtil.Loop(async () =>
			{
				foreach (ActiveChannel activeChannel in this._activeChannels)
				{
					bool isTyping = (await this._chieClient.IsTyping(activeChannel.ChieName)).IsTyping;

					activeChannel.SetTypingState(isTyping);
				}
			}, 5000, async ex => await this._logger.Write(ex));
		}
	}
}