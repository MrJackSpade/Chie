using ChieApi.Interfaces;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
    public class BootUpPipeline : IRequestPipeline
    {
        private readonly ICharacterFactory _characterFactory;

        private string? _characterName;

        private bool _firstMessage = true;

        public BootUpPipeline(ICharacterFactory characterFactory)
        {
            this._characterFactory = characterFactory;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            if (string.IsNullOrWhiteSpace(this._characterName))
            {
                this._characterName = (await this._characterFactory.Build()).CharacterName;
            }

            if (this._firstMessage)
            {
                yield return new ChatEntry()
                {
                    SourceUser = _characterName,
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