using Ai.Utils;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordGpt.Constants;

namespace DiscordGpt.Models
{
    public class ActiveMessage : IDisposable
    {
        private readonly ChangeSyncObject<string> _syncedContent;

        private bool _disposedValue;

        public ActiveMessage(RestUserMessage restUserMessage, long isReplyTo)
        {
            this.RestUserMessage = restUserMessage;
            this.IsReplyTo = isReplyTo;
            this._syncedContent = new(async s => await this.RestUserMessage.ModifyAsync(x => x.Content = s));
        }

        public long IsReplyTo { get; set; }

        public RestUserMessage RestUserMessage { get; set; }

        public async Task AddReact(string emoji) => await this.RestUserMessage.AddReactionAsync(Emoji.Parse(emoji));

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task RemoveReact(string? emoji = null)
        {
            if (emoji == null)
            {
                foreach (KeyValuePair<IEmote, ReactionMetadata> reaction in this.RestUserMessage.Reactions)
                {
                    await this.RestUserMessage.RemoveReactionAsync(reaction.Key, this.RestUserMessage.Author);
                }
            }
            else
            {
                await this.RestUserMessage.RemoveReactionAsync(Emoji.Parse(emoji), this.RestUserMessage.Author);
            }
        }

        public async Task SetContent(string content)
        {
            if (content.Length > 1800)
            {
                content = content[^1800..];
            }

            await this._syncedContent.Update(content);
        }

        public async Task SetUp()
        {
            await this.AddReact(Emojis.EYES);
            await this.AddReact(Emojis.STOP);
            await this.TryAddGif();
        }

        public async Task SetVisible(bool state)
        {
            if (!state)
            {
                await this._syncedContent.Update(null);
            }

            await this._syncedContent.ToggleChange(state);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    Task.Run(this.TearDown).Wait();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this._disposedValue = true;
            }
        }

        private async Task TearDown()
        {
            await this.RemoveReact();
            await this.AddReact(Emojis.GO);
            await this._syncedContent.Flush();
            await this.RestUserMessage.ModifyAsync(m => m.Attachments = new Optional<IEnumerable<FileAttachment>>(new List<FileAttachment>()));
        }

        private async Task TryAddGif()
        {
            if (this.RestUserMessage.Attachments.Count > 0)
            {
                return;
            }

            await this.RestUserMessage.ModifyAsync(r =>
            {
                List<FileAttachment> files = new()
                {
                    new FileAttachment(Files.TYPING_GIF)
                };

                r.Attachments = new Optional<IEnumerable<FileAttachment>>(files);
            });
        }
    }
}