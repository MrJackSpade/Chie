using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;
using ChieApi.Shared.Repositories;
using ChieApi.Shared.Services;
using Microsoft.Extensions.Logging;
using Summary;
using Summary.Models;

namespace UserSummarizer
{
	public class ChatSummarizer
	{
		private readonly ChatRepository _chatService;

		private readonly ILogger _logger;

		private readonly ChatSummarizerSettings _settings;

		private readonly SummaryApiClient _summaryClient;

		private readonly ModelRepository _modelRepository;

		private readonly SummarizationRepository _summarizationRepository;

		public ChatSummarizer(SummarizationRepository summarizationRepository, SummaryApiClient summaryClient, ILogger logger, ModelRepository modelRepository, ChatRepository chatService, ChatSummarizerSettings settings)
		{
			this._summarizationRepository = summarizationRepository;
			this._modelRepository = modelRepository;
			this._summaryClient = summaryClient;
			this._chatService = chatService;
			this._settings = settings;
			this._logger = logger;
		}

		public async Task Execute()
		{
			Model model = this._modelRepository.GetModel(this._settings.DefaultModel);

			long lastChatId = this._summarizationRepository.GetLastTokenizedChat(model.Id);

			ChatEntry? after = this._chatService.GetAfter(lastChatId);

			while (after != null)
			{
				Console.WriteLine($"Tokenizing: {after.Id}");
				TokenizeResponse response = await this._summaryClient.Tokenize($"{after.DisplayName}: {after.Content}");

				TokenCount count = new()
				{
					ChatEntryId = after.Id,
					Count = response.Content.Length,
					ModelId = model.Id
				};

				this._summarizationRepository.Add(count);

				after = this._chatService.GetAfter(after.Id);
			}
		}
	}
}