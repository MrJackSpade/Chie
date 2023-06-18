using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Services;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
    public class NameCleaningPipeline : IRequestPipeline
    {
        private readonly LogService _databaseService;

        public NameCleaningPipeline(LogService databaseService)
        {
            this._databaseService = databaseService;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            LlamaSafeString cleanedName = new(chatEntry.SourceUser);

            if (cleanedName.InvalidCharacters.Length > 0)
            {
                await this._databaseService.Log($"Removing characters: {string.Join(", ", cleanedName.InvalidCharacters)}");
            }

            if (cleanedName.IsNullOrWhitespace)
            {
                await this._databaseService.Log("Name empty.");
                yield break;
            }

            chatEntry.SourceUser = cleanedName.Content;

            yield return chatEntry;
        }
    }
}