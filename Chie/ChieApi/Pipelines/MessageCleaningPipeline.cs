using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Services;
using ChieApi.Shared.Entities;
using Microsoft.Extensions.Logging;

namespace ChieApi.Pipelines
{
    public class MessageCleaningPipeline : IRequestPipeline
    {
        private readonly ILogger _logger;

        public MessageCleaningPipeline(ILogger logger)
        {
            this._logger = logger;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            LlamaSafeString cleanedMessage = new(chatEntry.Content);

            if (cleanedMessage.InvalidCharacters.Length > 0)
            {
                this._logger.LogDebug($"Removing characters: {string.Join(", ", cleanedMessage.InvalidCharacters)}");
            }

            if (cleanedMessage.IsNullOrWhitespace)
            {
                this._logger.LogInformation("Message empty.");
                yield break;
            }

            chatEntry.Content = cleanedMessage.Content;

            yield return chatEntry;
        }
    }
}