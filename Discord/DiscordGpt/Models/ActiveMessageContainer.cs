﻿using Discord.Rest;
using Discord.WebSocket;
using DiscordGpt.Constants;
using DiscordGpt.Interfaces;

namespace DiscordGpt.Models
{
    public class ActiveMessageContainer : IActiveMessageContainer
    {
        private ActiveMessage _lastActiveMessage;

        public ActiveMessage? Value { get; private set; }

        public void Clear()
        {
            this.Value?.Dispose();
            this.Value = null;
        }

        public async Task Create(ISocketMessageChannel channel, long messageId, bool startVisible)
        {
            if (this._lastActiveMessage != null)
            {
                await this._lastActiveMessage.RemoveReact();
            }

            RestUserMessage message;

            if (!startVisible)
            {
                message = await channel.SendFileAsync(Files.TYPING_GIF);
            }
            else
            {
                message = await channel.SendMessageAsync(".");
            }

            this.Clear();

            ActiveMessage newActiveMessage = new(message, messageId);

            this.Value = newActiveMessage;

            await newActiveMessage.SetUp(startVisible);
        }

        public async Task Finalize(string content)
        {
            if (this.Value is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                await this.Value.SetContent(content);
                await this.Value.SetVisible(true);
            }

            this.Value.Dispose();

            if (!this.Value.Deleted)
            {
                this._lastActiveMessage = this.Value;
            }

            this.Value = null;
        }

        public async Task Open(ISocketMessageChannel channel, long chieMessageId, ulong discordMessageId, bool startVisible)
        {
            if (this._lastActiveMessage != null)
            {
                await this._lastActiveMessage.RemoveReact();
            }

            RestUserMessage message = await channel.GetMessageAsync(discordMessageId) as RestUserMessage;

            this.Clear();

            ActiveMessage newActiveMessage = new(message, chieMessageId, true);

            await newActiveMessage.SetContent(message.Content);

            this.Value = newActiveMessage;

            await newActiveMessage.SetUp(startVisible);
        }

        public void SetValue(ActiveMessage value) => this.Value = value;
    }
}