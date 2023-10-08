using ChieApi.Interfaces;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
    public class BootUpPipeline : IRequestPipeline
    {
        private readonly string _characterName;

        private bool _firstMessage = true;

        public BootUpPipeline(CharacterConfiguration characterConfiguration)
        {
            this._characterName = characterConfiguration.CharacterName;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            if (this._firstMessage)
            {
                yield return new ChatEntry()
                {
                    DisplayName = _characterName,
                    SourceChannel = chatEntry.SourceChannel,
                    IsVisible = false,
                    Content = "*abruptly regains consciousness*"
                };

                this._firstMessage = false;
            }

            yield return chatEntry;
        }
    }
}