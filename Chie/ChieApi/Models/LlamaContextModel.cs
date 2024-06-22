using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class LlamaContextModel : IAsyncEnumerable<LlamaToken>
    {
        private readonly LlamaTokenCache _cache;

        public LlamaContextModel(LlamaTokenCache cache)
        {
            this._cache = cache;
        }

        public ITokenCollection AssistantBlock { get; set; } = new LlamaTokenBlock();

        public ITokenCollection InstructionBlock { get; set; } = new LlamaTokenBlock();

        public List<ITokenCollection> Messages { get; } = new List<ITokenCollection>();

        public ITokenCollection Summary { get; set; } = new LlamaTokenBlock();

        public Dictionary<string, LlamaUserSummary> UserSummaries { get; } = new();

        private Task<IReadOnlyLlamaTokenCollection> NewLine => this._cache.Get("\n");

        public async Task<List<string>> GetActiveUsers()
        {
            HashSet<string> users = new();

            foreach (LlamaMessage lm in this.Messages.OfType<LlamaMessage>())
            {
                string n_string = (await lm.Header.Tokens).ToString();

                users.Add(n_string);
            }

            return users.ToList();
        }

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (LlamaToken token in this.GetAllTokens(this.InstructionBlock))
            {
                yield return token;
            }

            await foreach (LlamaToken token in this.GetAllTokens(this.Summary))
            {
                yield return token;
            }

            foreach (KeyValuePair<string, LlamaUserSummary> kvp in this.UserSummaries.OrderBy(k => k.Key))
            {
                await foreach (LlamaToken token in this.GetAllTokens(kvp.Value, true))
                {
                    yield return token;
                }
            }

            await foreach (LlamaToken token in this.GetAllTokens(this.AssistantBlock))
            {
                yield return token;
            }

            for (int i = 0; i < this.Messages.Count; i++)
            {
                ITokenCollection message = this.Messages[i];

                await foreach (LlamaToken token in this.GetAllTokens(message, i != this.Messages.Count - 1))
                {
                    yield return token;
                }
            }
        }

        public async Task<LlamaTokenCollection> GetState()
        {
            LlamaTokenCollection contextState = new();

            await foreach (LlamaToken token in this)
            {
                contextState.Append(token);
            }

            return contextState;
        }

        private async IAsyncEnumerable<LlamaToken> GetAllTokens(IAsyncEnumerable<LlamaToken> toReturn, bool andNewLine = true)
        {
            LlamaTokenCollection collection = await toReturn.ToCollection();

            if (collection.Count > 0)
            {
                foreach (LlamaToken token in collection)
                {
                    yield return token;
                }

                if (andNewLine)
                {
                    foreach (LlamaToken token in await this.NewLine)
                    {
                        yield return token;
                    }
                }
            }
        }
    }
}