using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Extensions;
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

        private Task<LlamaTokenCollection> NewLine => this._cache.Get("\n");

        public ITokenCollection Instruction { get; set; } = new LlamaTokenBlock();

        public ITokenCollection Summary { get; set; } = new LlamaTokenBlock();

        public List<ITokenCollection> Messages { get; } = new List<ITokenCollection>();

        private async IAsyncEnumerable<LlamaToken> AndNewLine(IAsyncEnumerable<LlamaToken> toReturn)
        {
            LlamaTokenCollection collection = await toReturn.ToCollection();

            if (collection.Count > 0)
            {
                foreach (LlamaToken token in collection)
                {
                    yield return token;
                }

                foreach (LlamaToken token in await this.NewLine)
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

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (LlamaToken token in this.AndNewLine(this.Instruction))
            {
                yield return token;
            }

            await foreach (LlamaToken token in this.AndNewLine(this.Summary))
            {
                yield return token;
            }

            foreach (ITokenCollection message in this.Messages)
            {
                await foreach (LlamaToken token in this.AndNewLine(message))
                {
                    yield return token;
                }
            }
        }
    }
}
