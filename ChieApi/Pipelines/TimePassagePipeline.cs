using ChieApi.Interfaces;
using ChieApi.Services;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
	public class TimePassagePipeline : IRequestPipeline
	{
		private readonly ICharacterFactory _characterFactory;
		private readonly ChatService _chatService;

		public TimePassagePipeline(ChatService databaseService, ICharacterFactory characterFactory)
		{
			this._chatService = databaseService;
			this._characterFactory = characterFactory;
		}

		public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
		{
			ChatEntry lastMessage = this._chatService.GetLastMessage();

			if (lastMessage != null)
			{
				TimeSpan sinceLast = DateTime.Now - lastMessage.DateCreated;

				double totalHours = sinceLast.TotalHours;

				if (totalHours > 1)
				{
					string timeSpan = this.GetTimeSpan(totalHours, out bool plural);

					string pos = plural ? "have" : "has";

					yield return new ChatEntry()
					{
						SourceUser = (await this._characterFactory.Build()).CharacterName,
						Content = $"*Notices {timeSpan} {pos} passed*"
					};
				}
			}

			yield return chatEntry;
		}

		private string GetTimeSpan(double totalHours, out bool plural)
		{
			string period;
			int count;

			TimeSpan sinceLast = TimeSpan.FromHours(totalHours);

			if (sinceLast.TotalDays < 1)
			{
				period = "hour";
				count = (int)sinceLast.TotalHours;
			}
			else
			{
				period = "day";
				count = (int)sinceLast.TotalDays;
			}

			plural = count > 1;

			if (plural)
			{
				period += "s";
			}

			return $"{count} {period}";
		}
	}
}