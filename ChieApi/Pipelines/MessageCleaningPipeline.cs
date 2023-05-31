using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Services;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
	public class MessageCleaningPipeline : IRequestPipeline
	{
		private readonly LogService _databaseService;

		public MessageCleaningPipeline(LogService databaseService)
		{
			this._databaseService = databaseService;
		}

		public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
		{
			LlamaSafeString cleanedMessage = new(chatEntry.Content);

			if (cleanedMessage.InvalidCharacters.Length > 0)
			{
				await this._databaseService.Log($"Removing characters: {string.Join(", ", cleanedMessage.InvalidCharacters)}");
			}

			if (cleanedMessage.IsNullOrWhitespace)
			{
				await this._databaseService.Log("Message empty.");
				yield break;
			}

			chatEntry.Content = cleanedMessage.Content;

			yield return chatEntry;
		}
	}
}