using Ai.Utils;
using Discord;
using Discord.Rest;
using DiscordGpt.Constants;
using System.Diagnostics;

namespace DiscordGpt.Models
{
    public class ActiveMessage : IDisposable
    {
        private readonly ChangeSyncObject<string> _syncedContent;

        private string _content;

        private bool _contentProvided;

        private bool _disposed;

        private bool _disposing = false;

        public ActiveMessage(RestUserMessage restUserMessage, long isReplyTo)
        {
            this.RestUserMessage = restUserMessage;
            this.IsReplyTo = isReplyTo;
            this._syncedContent = new(async s => await this.RestUserMessage.ModifyAsync(x => x.Content = s));
        }

        public bool Deleted { get; private set; }

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

        public async Task SetContent(string content, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException($"'{nameof(content)}' cannot be null or whitespace.", nameof(content));
            }

            this._contentProvided = true;

            this._content = content;

            if (content.Length > 1800)
            {
                content = content[^1800..];
            }

            await this._syncedContent.Update(content, force);
        }

        public async Task SetUp(bool startVisible)
        {
            if (!startVisible)
            {
                await this.AddReact(Emojis.EYES);
                await this.TryAddGif();
            }
            else
            {
                await this.SetVisible(true);
            }

            await this.AddReact(Emojis.STOP);
        }

        public async Task SetVisible(bool state)
        {
            if (this._disposing || this._disposed)
            {
                return;
            }

            if (!state)
            {
                await this._syncedContent.Update(null);
            }

            await this._syncedContent.ToggleChange(state);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                this._disposing = true;

                if (disposing)
                {
                    Task.Run(this.TearDown).Wait();
                }

                this._disposing = false;
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this._disposed = true;
            }
        }

        private async Task TearDown()
        {
            try
            {
                await this.RemoveReact();
                await this.AddReact(Emojis.GO);

                if (this._contentProvided)
                {
                    await this.SetContent(this._content, true);
                    await this.RestUserMessage.ModifyAsync(m => m.Attachments = new Optional<IEnumerable<FileAttachment>>(new List<FileAttachment>()));
                }
                else
                {
                    await this.RestUserMessage.DeleteAsync();
                    this.Deleted = true;
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
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