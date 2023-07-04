using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
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

            if (chatEntry.DisplayName != this._characterName &&
                !string.IsNullOrWhiteSpace(chatEntry.UserId) &&
                //Gotta check to make sure we haven't already returned this request
                this._returnedData.Add(chatEntry.UserId))
            {
                UserData? userData = this._userDataService.GetOrDefault(chatEntry.UserId);

                if (userData != null)
                {
                    if (userData.Blocked)
                    {
                        yield break;
                    }
                }

                //If prepend
                if (this.TryGetChatEntry(chatEntry, userData, out ChatEntry? ce1) && userData.BeforeMessage)
                {
                    yield return ce1;
                }

                string overrideName = this.Coalesce(userData?.DisplayName, chatEntry.DisplayName, chatEntry.UserId);
                    
                yield return chatEntry with {  DisplayName = overrideName };

                //If append
                if (this.TryGetChatEntry(chatEntry, userData, out ChatEntry ce2) && !userData.BeforeMessage)
                {
                    yield return ce2;
                }
            }
        }

        private string Coalesce(params string[] args)
        {
            foreach(string arg in args)
            {
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    return arg;
                }
            }

            throw new NotImplementedException();
        }

        public bool TryGetChatEntry(ChatEntry chatEntry, UserData userData, out ChatEntry? ce)
        {
            if (userData == null)
            {
                ce = null;
                return false;
            }

            if(string.IsNullOrWhiteSpace(userData.UserPrompt))
            {
                ce = null;
                return false;
            }

            ce = new ChatEntry()
            {
                DisplayName = _characterName,
                Content = userData.UserPrompt,
                IsVisible = false,
                SourceChannel = chatEntry.SourceChannel,
                Tag = LlamaTokenTags.TEMPORARY
            };

            return true;
        }
    }
}