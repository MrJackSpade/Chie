using ChieApi.Interfaces;
using ChieApi.Services;
using ChieApi.Shared.Entities;
using Llama.Constants;

namespace ChieApi.Tasks.Boredom
{
    public class BoredomTask : IBackgroundTask, IRequestPipeline
    {
        private readonly BoredomTaskData _data;

        private readonly LlamaService _llamaService;

        private readonly Random _random = new();

        private readonly BoredomTaskSettings _settings;

        public BoredomTask(LlamaService llamaService, BoredomTaskSettings settings, BoredomTaskData data)
        {
            this._settings = settings;
            this._llamaService = llamaService;
            this._data = data;
        }

        public async Task Initialize()
        {
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            if (!string.IsNullOrWhiteSpace(chatEntry.SourceChannel))
            {
                if (!this._data.MessageCounts.TryGetValue(chatEntry.SourceChannel, out int count))
                {
                    this._data.MessageCounts.Add(chatEntry.SourceChannel, 1);
                }
                else
                {
                    this._data.MessageCounts[chatEntry.SourceChannel]++;
                }

                this._data.LastMessage = DateTime.Now;
            }

            yield return chatEntry;
        }

        public async Task TickMinute()
        {
            if (this._random.NextDouble() > this._settings.Probability)
            {
                return;
            }

            if (!this._data.MessageCounts.Any())
            {
                return;
            }

            int elapsedMinutes = (int)(DateTime.Now - this._data.LastMessage).TotalMinutes;
            BoredomTaskAction[] actionTargets = this._settings.TaskActions.Where(t => t.StartMinutes <= elapsedMinutes && t.EndMinutes > elapsedMinutes).ToArray();

            if (!actionTargets.Any())
            {
                return;
            }

            BoredomTaskAction selectedAction = actionTargets[this._random.Next(actionTargets.Length)];

            string highestVolumeChannel = this._data.MessageCounts.OrderByDescending(k => k.Value).FirstOrDefault().Key;

            await this._llamaService.Initialization;

            await this._llamaService.Send(new ChatEntry()
            {
                SourceChannel = highestVolumeChannel,
                Content = selectedAction.Text,
                Tag = LlamaTokenTags.TEMPORARY,
                IsVisible = false,
                DisplayName = this._llamaService.CharacterName
            });
        }
    }
}