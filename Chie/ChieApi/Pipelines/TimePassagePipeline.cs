using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;

namespace ChieApi.Pipelines
{
    public class TimePassagePipeline : IRequestPipeline
    {
        private readonly ChatRepository _chatService;

        private readonly string _characterName;

        public TimePassagePipeline(ChatRepository databaseService, CharacterConfiguration characterConfiguration)
        {
            this._chatService = databaseService;
            this._characterName = characterConfiguration.CharacterName;
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
                        DisplayName = _characterName,
                        Content = $"*Notices {timeSpan} {pos} passed*",
                        Type = LlamaTokenType.Temporary
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