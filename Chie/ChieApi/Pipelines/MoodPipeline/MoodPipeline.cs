using ChieApi.Interfaces;
using ChieApi.Services;
using ChieApi.Shared.Entities;
using Llama.Constants;

namespace ChieApi.Pipelines.MoodPipeline
{
    public class MoodPipeline : IRequestPipeline
    {
        private readonly Queue<DateTime> _cadenceQueue = new();

        private readonly LlamaService _llamaService;

        private readonly Random _random = new();

        private readonly object _registerLock = new();

        private readonly MoodPipelineSettings _settings;

        private DateTime _lastMoodSent = DateTime.MinValue;

        public MoodPipeline(MoodPipelineSettings settings, LlamaService llamaService)
        {
            this._settings = settings;
            this._llamaService = llamaService;
        }

        public bool IsFirstMessage => this._settings.FirstMessage && this._lastMoodSent == DateTime.MinValue;

        private int CurrentCadence
        {
            get
            {
                lock (this._registerLock)
                {
                    int toReturn = 0;

                    if (this._cadenceQueue.Count < 2)
                    {
                        return 0;
                    }

                    DateTime startMessage = this._cadenceQueue.First();

                    for (int i = 1; i < this._cadenceQueue.Count; i++)
                    {
                        toReturn += (int)(this._cadenceQueue.ElementAt(i) - startMessage).TotalSeconds;
                    }

                    toReturn /= this._cadenceQueue.Count - 1;

                    return toReturn;
                }
            }
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            yield return chatEntry;

            this.TryRegisterMessage();

            if (!string.IsNullOrWhiteSpace(this._llamaService.CharacterName) && this.ValidTime())
            {
                bool rolled = this.TryRoll(out MoodPipelineEvent e, this.IsFirstMessage);

                if (rolled)
                {
                    yield return chatEntry with
                    {
                        Content = e.Text,
                        Image = Array.Empty<byte>(),
                        IsVisible = false,
                        DisplayName = this._llamaService.CharacterName,
                        Tag = LlamaTokenTags.TEMPORARY
                    };

                    this._lastMoodSent = DateTime.Now;
                }
            }
        }

        public bool TryRoll(out MoodPipelineEvent e, bool force)
        {
            do
            {
                foreach (MoodPipelineEvent moodPipelineEvent in this._settings.Events)
                {
                    if (this._random.NextDouble() < moodPipelineEvent.Chance)
                    {
                        e = moodPipelineEvent;
                        return true;
                    }
                }
            } while (force);

            e = null;
            return false;
        }

        public bool ValidTime()
        {
            if (this.IsFirstMessage)
            {
                return true;
            }

            if (this.CurrentCadence < this._settings.MinCadenceSeconds)
            {
                return false;
            }

            if ((DateTime.Now - this._lastMoodSent).TotalMinutes > this._settings.MinDelayMinutes)
            {
                return true;
            }

            return false;
        }

        private void TryRegisterMessage()
        {
            lock (this._registerLock)
            {
                DateTime last = DateTime.MinValue;

                if (this._cadenceQueue.Any())
                {
                    last = this._cadenceQueue.Last();
                }

                if ((DateTime.Now - last).TotalMinutes > 1)
                {
                    this._cadenceQueue.Enqueue(DateTime.Now);
                }

                while (this._cadenceQueue.Count > this._settings.CadenceQueueSize)
                {
                    _ = this._cadenceQueue.TryDequeue(out _);
                }

                last = DateTime.Now;
            }
        }
    }
}