﻿using Ai.Utils;
using Ai.Utils.Extensions;
using Chie;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Interfaces;
using ChieApi.Shared.Models;
using Discord;
using Discord.WebSocket;
using DiscordGpt.Constants;
using DiscordGpt.EmojiReactions;
using DiscordGpt.Events;
using DiscordGpt.Extensions;
using DiscordGpt.Interfaces;
using DiscordGpt.Models;
using DiscordGpt.Services;
using DiscordGpt.Utils;
using Embedding;
using Logging.Shared.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DiscordGpt
{
    internal class DiscordIntegrationService
    {
        private readonly ActiveChannelCollection _activeChannels;

        private readonly IActiveMessageContainer _activeMessageContainer;

        private readonly IChieClient _chieClient;

        private readonly ChieMessageService _chieMessageService;

        private readonly DiscordClient _discordClient;

        private readonly EmbeddingApiClient _embeddingApiClient;

        private readonly Dictionary<ulong, Dictionary<string, string>> _guildEmotes = new();

        private readonly ILogger _logger;

        private readonly NameService _nameService;

        private readonly ReactionActionCollection _reactionActionCollection;

        private readonly DiscordIntegrationSettings _settings;

        private readonly StartInfo _startInfo;

        //private float[] _lastEmbedding = null;

        private Task? _receiveTask;

        private bool _startVisible;

        private Task? _typingTask;

        public DiscordIntegrationService(IChieClient chieClient, EmbeddingApiClient embeddingApiClient, IActiveMessageContainer activeMessagecontainer, IEnumerable<IReactionAction> reactionActions, ActiveChannelCollection activeChannelCollection, NameService nameService, ChieMessageService messageService, StartInfo startInfo, DiscordClient discordClient, ILogger logger, DiscordIntegrationSettings settings)
        {
            this._embeddingApiClient = embeddingApiClient;
            this._activeMessageContainer = activeMessagecontainer;
            this._chieClient = chieClient;
            this._nameService = nameService;
            this._activeChannels = activeChannelCollection;
            this._chieMessageService = messageService;
            this._chieMessageService.OnMessagesSent += this._chieMessageService_OnMessagesSent;
            this._startInfo = startInfo;
            this._settings = settings;
            this._logger = logger;
            this._discordClient = discordClient;
            this._reactionActionCollection = new(reactionActions);
            this._discordClient.OnReactionAdded += this.OnReactionAdded;
            this._discordClient.OnTypingStart += this.OnTypingStart;
            this._discordClient.OnReactionRemoved += this.OnReactionRemoved;

            foreach (ulong channelId in settings.PublicChannels)
            {
                Console.WriteLine("Monitoring Channel: " + channelId);
            }
        }

        public static float CosineSimilarity(float[] vector1, float[] vector2)
        {
            float dotProduct = 0.0f;
            float normA = 0.0f;
            float normB = 0.0f;
            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                normA += vector1[i] * vector1[i];
                normB += vector2[i] * vector2[i];
            }

            return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        public static float EuclideanDistance(float[] vector1, float[] vector2)
        {
            float sum = 0.0f;
            for (int i = 0; i < vector1.Length; i++)
            {
                sum += (vector1[i] - vector2[i]) * (vector1[i] - vector2[i]);
            }

            return (float)Math.Sqrt(sum);
        }

        public async Task ProcessIncomingMessage(ChatEntry messageResponse)
        {
            this._logger.LogInformation("Response Received. Cleaning...");

            string cleanedMessage = messageResponse.Content;

            this._logger.LogInformation($"Generating Embedding");

            //try
            //{
            //    EmbeddingResponse thisEmbeddingResponse = await this._embeddingApiClient.Generate(new EmbeddingRequest() { TextData = new string[] { cleanedMessage } });

            //    if (thisEmbeddingResponse != null)
            //    {
            //        float[] thisEmbedding = thisEmbeddingResponse.Content[0];

            //        if (_lastEmbedding != null)
            //        {
            //            float similarity = CosineSimilarity(thisEmbedding, _lastEmbedding);
            //            float distance = EuclideanDistance(thisEmbedding, _lastEmbedding);

            //            cleanedMessage = $"[{distance:0.00}] {cleanedMessage}";
            //        }

            //        _lastEmbedding = thisEmbedding;
            //    }
            //} catch(Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            ActiveChannel activeChannel = this._activeChannels[messageResponse.SourceChannel];

            if (activeChannel is null)
            {
                this._logger.LogWarning($"Active channel not found for message {messageResponse.Id}");
                return;
            }

            if (this._settings.UseServerEmotes && activeChannel.Channel is SocketTextChannel stcb)
            {
                cleanedMessage = this.EmojiFill(stcb, cleanedMessage);
            }

            cleanedMessage = cleanedMessage.DiscordEscape();

            this._logger.LogInformation("Sending to chat...");

            this._logger.LogTrace($"Message: {cleanedMessage}");

            bool useExistingMessage = this._activeMessageContainer.Value?.IsReplyTo == messageResponse.ReplyToId;

            foreach (string part in cleanedMessage.SplitLength(1800))
            {
                if (useExistingMessage)
                {
                    await this._activeMessageContainer.Finalize(part);
                    useExistingMessage = false;
                }
                else
                {
                    _ = await activeChannel.Channel.SendMessageAsync(part);
                }
            }

            //now that we've recieved a response, we can allow new messages in
            this._chieMessageService.EnableSend = true;
        }

        public async Task Start()
        {
            Console.WriteLine("Connecting Discord...");
            await this._discordClient.Connect();
            Console.WriteLine("Connected Discord.");

            this._reactionActionCollection.SetOwner(this._discordClient.CurrentUser.Username);
            this._discordClient.OnMessageReceived += this.Client_OnMessageReceived;

            this._typingTask = Task.Run(this.TypingLoop);
            this._receiveTask = Task.Run(this.ReceiveLoop);

            this._startVisible = (await _chieClient.StartVisible()).StartVisible;

            await LoopUtil.Forever();
        }

        private async Task _chieMessageService_OnMessagesSent(ChieMessageSendEvent obj)
        {
            //wait for response before allowing new messages to go out
            this._chieMessageService.EnableSend = false;

            await this._activeMessageContainer.Create(obj.Messages.Last().SocketMessage.Channel, obj.MessageId, this._startVisible);
        }

        private async Task<IncomingDiscordMessage> BuildDiscordMessage(SocketMessage arg)
        {
            string messageContent = this._chieMessageService.CleanContent(arg.Content);

            foreach (SocketUser user in arg.MentionedUsers)
            {
                string placeholder = $"<@{user.Id}>";
                messageContent = messageContent.Replace(placeholder, $"*at <@{user.Username}>*");
            }

            foreach (SocketRole role in arg.MentionedRoles)
            {
                string placeholder = $"<@&{role.Id}>";
                messageContent = messageContent.Replace(placeholder, $"*at {role.Name}*");
            }

            messageContent = messageContent.Replace($"<@&{_discordClient.CurrentUser.Id}>", $"*at <@{_discordClient.CurrentUser.GetDisplayName()}>*");

            string userCleaned = this._nameService.CleanUserName(arg.Author.GetDisplayName());

            ActiveChannel activeChannel = this._activeChannels.GetOrAdd(arg.Channel);

            IncomingDiscordMessage message = new()
            {
                DisplayName = userCleaned,
                UserId = arg.Author.Username,
                Channel = activeChannel.ChieName,
                Content = messageContent,
                Images = await arg.GetImages().ToListAsync(),
                SocketMessage = arg
            };

            //Move this logic somewhere else
            while (message.Content.Trim().StartsWith("&"))
            {
                string command = messageContent[..messageContent.IndexOf(' ')][1..];

                string k = command.Split('=')[0];
                string v = command.Split('=')[1];

                message.Content = messageContent[(messageContent.IndexOf(' ') + 1)..];

                if (string.Equals(userCleaned, this._settings.AdminUser, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(k, "username", StringComparison.OrdinalIgnoreCase))
                    {
                        message.UserId = v;
                        message.DisplayName = v;
                    }
                }
            }

            return message;
        }

        private async Task Client_OnMessageReceived(SocketMessage arg)
        {
            if (arg.Author.Username == this._discordClient.CurrentUser.Username)
            {
                this._logger.LogInformation("Self Message. Skipping.");
                return;
            }

            this._logger.LogInformation($"Received Message on Channel [{arg.Channel.Id}]");

            if (!arg.IsVisible())
            {
                this._logger.LogInformation("Message not visible. Marking.");
                await this._chieMessageService.MarkUnseen(arg);
                return;
            }

            bool isAdmin = string.Equals(arg.Author.Username, this._settings.AdminUser, StringComparison.OrdinalIgnoreCase);
            bool allowDms = this._settings.AllowDms || (isAdmin && this._settings.AllowAdminDms);

            if (arg.Channel is SocketDMChannel && !allowDms)
            {
                this._logger.LogInformation("Channel is DM but DM's are disabled");
                return;
            }

            if (!this._settings.PublicChannels.Contains(arg.Channel.Id) && arg.Channel is not SocketDMChannel)
            {
                this._logger.LogInformation("Message not on visible channel. Skipping");
                return;
            }

            if (arg.Reference != null && arg.Reference.MessageId.IsSpecified)
            {
                ulong messageId = arg.Reference.MessageId.Value;
                ulong channelId = arg.Reference.ChannelId;

                IMessage message = await this._discordClient.GetMessage(channelId, messageId);

                if (message.Author.Username != this._discordClient.CurrentUser.Username)
                {
                    this._logger.LogInformation("Response to another users message");
                    await this._chieMessageService.MarkUnseen(arg);
                    return;
                }
            }

            _ = Task.Run(async () =>
            {
                IncomingDiscordMessage incomingDiscordMessage = await this.BuildDiscordMessage(arg);

                await this._chieMessageService.DeferredMessageProcessing(incomingDiscordMessage);
            });
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

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            ReactionData? data = await ReactionDataParser.GetDataAsync(cachedMessage, cachedChannel, reaction);

            if (data is null)
            {
                return;
            }

            await this._reactionActionCollection.AddReaction(data.Name, data.RemainingCount, data.ReactedUser, data.ReactedMessage);
        }

        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            ReactionData? data = await ReactionDataParser.GetDataAsync(cachedMessage, cachedChannel, reaction);

            if (data is null)
            {
                return;
            }

            await this._reactionActionCollection.RemoveReaction(data.Name, data.RemainingCount, data.ReactedUser, data.ReactedMessage);
        }

        private async Task OnTypingStart(Cacheable<IUser, ulong> cachedUser, Cacheable<IMessageChannel, ulong> cachedChannel)
        {
            if (!this._activeChannels.TryGetValue(cachedChannel.Id, out ActiveChannel? activeChannel))
            {
                return;
            }

            this._chieMessageService.TryDelaySend(DateTime.Now.AddSeconds(10));
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
                        if (chatEntry.DateCreated < this._startInfo.StartTime)
                        {
                            continue;
                        }

                        await this.ProcessIncomingMessage(chatEntry);

                        activeChannel.LastMessageId = chatEntry.Id;
                    }
                }
            }, 1000, this._logger.LogError);
        }

        private async Task TypingLoop()
        {
            await LoopUtil.Loop(async () =>
            {
                foreach (ActiveChannel activeChannel in this._activeChannels)
                {
                    IsTypingResponse typingResponse = (await this._chieClient.IsTyping(activeChannel.ChieName));

                    if (typingResponse.IsTyping && !string.IsNullOrWhiteSpace(typingResponse.Content))
                    {
                        if (true)
                        {
                            this._activeMessageContainer.Value?.SetContent(typingResponse.Content);
                        }
                        else
                        {
                            this._activeMessageContainer.Value?.SetContent($"*{Emojis.KEYBOARD}*" + typingResponse.Content);
                        }
                    }

                    activeChannel.SetTypingState(typingResponse.IsTyping);
                }
            }, 5000, async ex => this._logger.LogError(ex));
        }
    }
}