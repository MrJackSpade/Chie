﻿using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using System.Text.RegularExpressions;

namespace ChieApi.Pipelines
{
    public partial class UserDataPipeline : IRequestPipeline
    {
        private readonly CharacterConfiguration _characterConfiguration;

        private readonly HashSet<string> _returnedData = new();

        private readonly UserDataRepository _userDataService;

        public UserDataPipeline(UserDataRepository userDataService, CharacterConfiguration characterConfiguration)
        {
            this._userDataService = userDataService;
            this._characterConfiguration = characterConfiguration;
        }

        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            this.SwapId(chatEntry);

            if (chatEntry.DisplayName != this._characterConfiguration.CharacterName &&
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
                DisplayName = null,
                Content = displayText.Content,
                IsVisible = false,
                SourceChannel = chatEntry.SourceChannel,
                Type = displayText.Type
            };

            return true;
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

        private TextResult GetText(UserData userData)
        {
            string? prompt = this._characterConfiguration?.UserPrompt ?? userData?.UserPrompt;

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                return new TextResult(prompt, LlamaTokenType.Temporary);
            }

            if (!string.IsNullOrWhiteSpace(userData.UserSummary) && userData.LastEncountered.HasValue)
            {
                int minutes = (int)(DateTime.Now - userData.LastEncountered.Value).TotalMinutes;

                string displayName = this.Coalesce(userData?.DisplayName, userData.UserId);

                if (minutes > 60)
                {
                    return new TextResult($"[{displayName} returns]", LlamaTokenType.Input);
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
    }
}