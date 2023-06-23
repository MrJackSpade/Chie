using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Services;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
    public class NameCleaningPipeline : IRequestPipeline
    {
        private readonly ILogger _logger;

        public NameCleaningPipeline(ILogger logger)
        {
            this._logger = logger;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            LlamaSafeString cleanedName = new(chatEntry.SourceUser);

            if (cleanedName.InvalidCharacters.Length > 0)
            {
                this._logger.LogInformation($"Removing characters: {string.Join(", ", cleanedName.InvalidCharacters)}");
            }

            if (cleanedName.IsNullOrWhitespace)
            {
                this._logger.LogError("Name empty.");
                yield break;
            }

            chatEntry.SourceUser = cleanedName.Content;

            yield return chatEntry;
        }
    }
}