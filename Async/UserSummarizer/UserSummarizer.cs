using ChieApi;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using Llama;
using Llama.Collections;
using Llama.Context;
using Llama.Data;
using Llama.Extensions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace UserSummarizer
{
    public class UserSummarizer
    {
        private readonly ChatService _chatService;

        private readonly ILogger _logger;

        private readonly UserSummarizerSettings _settings;

        private readonly UserDataService _userDataService;

        private ContextEvaluator _context;

        public UserSummarizer(ILogger logger, ChatService chatService, UserDataService userDataService, UserSummarizerSettings settings)
        {
            this._chatService = chatService;
            this._userDataService = userDataService;
            this._settings = settings;
            this._logger = logger;
        }

        private string GetDisplayName(UserData userData)
        {
            string displayName = userData.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = userData.UserId;
            }

            StringBuilder toReturn = new();

            foreach(char c in displayName)
            {
                if(char.IsLetterOrDigit(c))
                {
                    toReturn.Append(c);
                }
            }
            
            return toReturn.ToString();
        }
        public async Task Execute()
        {
            this._context = new ContextEvaluatorBuilder(this._settings).BuildUserSummaryEvaluator();

            do
            {
                foreach (string userId in this._chatService.GetUserIds())
                {
                    this._context.Clear();

                    UserData userData = await this._userDataService.GetOrCreate(userId);

                    string displayName = this.GetDisplayName(userData);

                    LlamaTokenCollection summarizeSuffix = this._context.Tokenize($"\nHuman: Please describe {displayName} as a person\nAssistant: {displayName} is");

                    this._logger.LogInformation("Summarizing User: " + userId);

                    ChatEntry lastMessage = this._chatService.GetLastMessage(userId);

                    if (lastMessage is null || lastMessage.Id < userData.LastChatId + 10)
                    {
                        continue;
                    }

                    LlamaTokenCollection llamaTokens = new();
                    LlamaTokenCollection summaryTokens = this.TryGetTokens(userData.UserSummary);

                    llamaTokens.Append(LlamaToken.Bos);
                    llamaTokens.Append(summaryTokens);

                    IEnumerable<ChatEntry> nextLastMessage = this._chatService.GetLastMessages(userId);

                    MessageToSummarizeCollection messages = new(this._context);
                    messages.Add($"{displayName}: ");
                    messages.AddRange(nextLastMessage);

                    if (messages.Count > 10)
                    {
                        foreach (LlamaTokenCollection message in messages.TokenizedMessages.Reverse())
                        {
                            llamaTokens.Append(message);
                            llamaTokens.Append(new LlamaToken(13, IntPtr.Zero, ""));
                        }

                        llamaTokens = llamaTokens.Replace(this._context.Tokenize("\n"), this._context.Tokenize(" "));

                        llamaTokens.Append(summarizeSuffix);

                        LlamaTokenCollection result = new();

                        foreach (LlamaToken resultToken in this._context.Call(llamaTokens))
                        {
                            result.Append(resultToken);
                        }

                        userData.UserSummary = result.ToString().Replace("User:", "", StringComparison.OrdinalIgnoreCase).Trim();
                        userData.LastChatId = messages.LastMessageId;

                        await _userDataService.Save(userData);
                    }
                }

                await Task.Delay(30_000);
            } while (true);
        }

        private LlamaTokenCollection TryGetTokens(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return new LlamaTokenCollection();
            }

            return this._context.Tokenize(s);
        }
    }
}