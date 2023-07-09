using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using Llama.Constants;
using Loxifi;
using System.Text.RegularExpressions;

namespace ChieApi.Pipelines
{
    public partial class UserDataPipeline : IRequestPipeline
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

        private TextResult GetText(UserData userData)
        {
            if (!string.IsNullOrWhiteSpace(userData.UserPrompt))
            {
                return new TextResult(userData.UserPrompt, LlamaTokenTags.TEMPORARY);
            }

            if (!string.IsNullOrWhiteSpace(userData.UserSummary) && userData.LastEncountered.HasValue)
            {
                int minutes = (int)(DateTime.Now - userData.LastEncountered.Value).TotalMinutes;

                string displayName = this.Coalesce(userData?.DisplayName, userData.UserId);

                if (minutes > 60)
                {
                    return new TextResult($"*notices her friend {displayName} arrive. {userData.UserSummary}]", LlamaTokenTags.STAGE_DIRECTION);
                } else
                {
                    return new TextResult($"*remembers that {userData.UserSummary.To(". ")}*", LlamaTokenTags.TEMPORARY);
                }
            }

            return new TextResult();
        }

        private void SwapId(ChatEntry chatEntry)
        {
            if (!string.IsNullOrWhiteSpace(chatEntry.Content))
            {
                foreach (Match m in Regex.Matches(chatEntry.Content, "<@(.*?)>"))
                {
                    string userId = m.Groups[1].Value;

                    UserData? userData = this._userDataService.GetOrDefault(userId);

                    if (userData != null && !string.IsNullOrWhiteSpace(userData.DisplayName))
                    {
                        chatEntry.Content = chatEntry.Content.Replace(m.Groups[0].Value, userData.DisplayName);
                    }
                }
            }
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            if (string.IsNullOrWhiteSpace(this._characterName))
            {
                this._characterName = (await this._characterFactory.Build()).CharacterName;
            }

            this.SwapId(chatEntry);

            if (chatEntry.DisplayName != this._characterName &&
                !string.IsNullOrWhiteSpace(chatEntry.UserId) &&
                //Gotta check to make sure we haven't already returned this request
                this._returnedData.Add(chatEntry.UserId))
            {
                UserData userData = await this._userDataService.GetOrCreate(chatEntry.UserId);
                this._userDataService.Encounter(chatEntry.UserId);

                if (userData.Blocked)
                {
                    yield break;
                }

                //If prepend
                if (this.TryGetChatEntry(chatEntry, userData, out ChatEntry? ce1) && userData.BeforeMessage)
                {
                    yield return ce1;
                }

                string overrideName = this.Coalesce(userData?.DisplayName, chatEntry.DisplayName, chatEntry.UserId);

                yield return chatEntry with { DisplayName = overrideName };

                //If append
                if (this.TryGetChatEntry(chatEntry, userData, out ChatEntry ce2) && !userData.BeforeMessage)
                {
                    yield return ce2;
                }
            }
        }

        private string Coalesce(params string[] args)
        {
            foreach (string arg in args)
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
            if (userData == null || userData.IsBot)
            {
                ce = null;
                return false;
            }

            TextResult displayText = this.GetText(userData);

            if (!displayText.HasValue)
            {
                ce = null;
                return false;
            }

            ce = new ChatEntry()
            {
                DisplayName = _characterName,
                Content = displayText.Content,
                IsVisible = false,
                SourceChannel = chatEntry.SourceChannel,
                Tag = displayText.Tag
            };

            return true;
        }
    }
}