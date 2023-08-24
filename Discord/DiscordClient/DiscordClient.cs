using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Chie
{
    public class DiscordClient
    {
        private static readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
        });

        private static readonly TaskCompletionSource<bool> _discordReady = new TaskCompletionSource<bool>();

        private readonly AutoResetEvent _connectionGate = new AutoResetEvent(true);

        private readonly DiscordClientSettings _settings;

        public DiscordClient(DiscordClientSettings settings)
        {
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public event Func<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> OnMessageDeleted;

        public event Func<SocketMessage, Task> OnMessageReceived;

        public event Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> OnReactionAdded;

        public event Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> OnReactionRemoved;

        public event Func<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>, Task> OnTypingStart;

        public bool Connected { get; private set; }

        public SocketSelfUser CurrentUser => _client.CurrentUser;

        public async Task Connect()
        {
            this._connectionGate.WaitOne();

            if (!this.Connected)
            {
                _client.Log += Client_Log;

                await _client.LoginAsync(TokenType.Bot, this._settings.Token);
                await _client.StartAsync();
                _client.Ready += ClientReady;

                _ = await _discordReady.Task;

                this.Connected = true;

                await _client.CurrentUser.ModifyAsync(c => c.Username = this._settings.UserName);

                _client.MessageReceived += (s) => OnMessageReceived?.Invoke(s);
                _client.ReactionAdded += (a, b, c) => OnReactionAdded?.Invoke(a, b, c);
                _client.MessageDeleted += (a, b) => OnMessageDeleted?.Invoke(a, b);
                _client.UserIsTyping += (a, b) => OnTypingStart?.Invoke(a, b);
                _client.ReactionRemoved += (a, b, c) => OnReactionRemoved?.Invoke(a, b, c);
            }

            this._connectionGate.Set();
        }

        public SocketTextChannel GetChannel(ulong channelId) => (SocketTextChannel)_client.GetChannel(channelId);

        public async Task<IGuildUser> GetGuildUser(RestUserMessage restUserMessage)
        {
            if (restUserMessage.Channel is SocketTextChannel stc)
            {
                await stc.Guild.DownloadUsersAsync();
                IGuildUser user = stc.Guild.GetUser(restUserMessage.Author.Id);
                return user;
            }

            return null;
        }

        public Task<IMessage> GetMessage(ulong channelId, ulong messageId) => this.GetChannel(channelId).GetMessageAsync(messageId);

        public IEnumerable<IThreadChannel> GetThreads(ITextChannel threadRoot)
        {
            List<SocketGuildChannel> channels = _client.GetGuild(threadRoot.GuildId).Channels.ToList();

            foreach (IChannel c in channels)
            {
                ChannelType channelType = (ChannelType)c.GetChannelType();

                if (channelType == ChannelType.PublicThread)
                {
                    yield return c as IThreadChannel;
                }
            }
        }

        public async Task LockChannel(SocketTextChannel channel)
        {
            OverwritePermissions permissions = new OverwritePermissions(sendMessages: PermValue.Deny); // Deny the permission to send messages

            // Overwrite the permissions for everyone role
            await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole, permissions);
        }

        public async Task UnlockChannel(SocketTextChannel channel)
        {
            OverwritePermissions permissions = new OverwritePermissions(sendMessages: PermValue.Allow); // Deny the permission to send messages

            // Overwrite the permissions for everyone role
            await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole, permissions);
        }

        private static Task Client_Log(LogMessage arg)
        {
            Debug.WriteLine(arg.Message);
            return Task.CompletedTask;
        }

        private static async Task ClientReady()
        {
            await Task.Delay(1);

            if (!_discordReady.Task.IsCompleted)
            {
                _discordReady.SetResult(true);
            }
        }
    }
}