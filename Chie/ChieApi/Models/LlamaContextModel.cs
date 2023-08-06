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

        public ITokenCollection Instruction { get; set; } = new LlamaTokenBlock();

        public List<ITokenCollection> Messages { get; } = new List<ITokenCollection>();

        public ITokenCollection Summary { get; set; } = new LlamaTokenBlock();

        private Task<IReadOnlyLlamaTokenCollection> NewLine => this._cache.Get("\n");

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (LlamaToken token in this.GetAllTokens(this.Instruction))
            {
                yield return token;
            }

            await foreach (LlamaToken token in this.GetAllTokens(this.Summary))
            {
                yield return token;
            }

            for(int i = 0; i < this.Messages.Count; i++) 
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