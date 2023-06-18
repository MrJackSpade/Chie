using ChieApi.Interfaces;
using ChieApi.Services;
using ChieApi.Shared.Entities;
using Llama.Constants;

namespace ChieApi.Pipelines
{
    public class UserDataPipeline : IRequestPipeline
    {
        private readonly ICharacterFactory _characterFactory;

        private readonly HashSet<string> _returnedData = new();

        private readonly UserDataService _userDataService;

        private string? _characterName;

        public UserDataPipeline(UserDataService userDataService, ICharacterFactory characterFactory)
        {
            this._userDataService = userDataService;
            this._characterFactory = characterFactory;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            if (string.IsNullOrWhiteSpace(this._characterName))
            {
                this._characterName = (await this._characterFactory.Build()).CharacterName;
            }

            if (chatEntry.SourceUser != this._characterName &&
                !string.IsNullOrWhiteSpace(chatEntry.SourceUser) &&
                //Gotta check to make sure we haven't already returnedthis request
                this._returnedData.Add(chatEntry.SourceUser))
            {
                UserData userData = this._userDataService.GetUserData(chatEntry.SourceUser);

                yield return new ChatEntry()
                {
                    SourceUser = _characterName,
                    Content = userData.UserPrompt,
                    IsVisible = false,
                    SourceChannel = chatEntry.SourceChannel,
                    Tag = LlamaTokenTags.TEMPORARY
                };
            }

            yield return chatEntry;
        }
    }
}