using Discord.Rest;

namespace DiscordGpt.Models
{
    internal class ActiveMessage
    {
        private string _content;

        public ActiveMessage(RestUserMessage restUserMessage, long isReplyTo)
        {
            this.RestUserMessage = restUserMessage;
            this.IsReplyTo = isReplyTo;
        }

        public long IsReplyTo { get; set; }

        public RestUserMessage RestUserMessage { get; set; }

        public async Task SetContent(string content)
        {
            if (content.Length > 1800)
            {
                content = content[^1800..];
            }

            if (content != this._content && !string.IsNullOrWhiteSpace(content))
            {
                await this.RestUserMessage.ModifyAsync(x => x.Content = content);
                this._content = content;
            }
        }
    }
}