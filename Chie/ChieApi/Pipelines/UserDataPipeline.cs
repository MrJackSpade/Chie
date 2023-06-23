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

        public bool TryGetChatEntry(ChatEntry chatEntry, UserData userData, out ChatEntry ce)
        {
            if (userData == null)
            {
                ce = null;
                return false;
            }

            ce = new ChatEntry()
            {
                SourceUser = _characterName,
                Content = userData.UserPrompt,
                IsVisible = false,
                SourceChannel = chatEntry.SourceChannel,
                Tag = LlamaTokenTags.TEMPORARY
            };

            return true;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            if (string.IsNullOrWhiteSpace(this._characterName))
            {
                this._characterName = (await this._characterFactory.Build()).CharacterName;
            }

            if (chatEntry.SourceUser != this._characterName &&
                !string.IsNullOrWhiteSpace(chatEntry.SourceUser) &&
                //Gotta check to make sure we haven't already returned this request
                this._returnedData.Add(chatEntry.SourceUser))
            {
                UserData? userData = this._userDataService.GetUserData(chatEntry.SourceUser);

                if(userData != null)
                {
                    if(userData.Blocked)
                    {
                        yield break;
                    }
                }

                //If prepend
                if (this.TryGetChatEntry(chatEntry, userData, out ChatEntry ce1) && userData.BeforeMessage)
                {
                    yield return ce1;
                }

                yield return chatEntry;

                //If append
                if (this.TryGetChatEntry(chatEntry, userData, out ChatEntry ce2) && !userData.BeforeMessage)
                {
                    yield return ce2;
                }
            }
        }
    }
}