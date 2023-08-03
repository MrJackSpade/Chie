using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class LlamaTokenBlock : ITokenCollection
    {
        private LlamaTokenCollection _tokens;

        public LlamaTokenBlock()
        {
            this.Type = LlamaTokenType.Undefined;
        }

        public LlamaTokenBlock(string content, LlamaTokenType type, LlamaTokenCache cache)
        {
            this.Content = new CachedTokenCollection(content, cache, true);
            this.Type = type;
        }

        public LlamaTokenBlock(IReadOnlyLlamaTokenCollection content, LlamaTokenType type)
        {
            this.Content = new CachedTokenCollection(content);
            this.Type = type;
        }

        public CachedTokenCollection Content { get; private set; }

        public LlamaTokenType Type { get; private set; }
        public long Id { get; }

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await this.EnsureTokens();

            foreach (LlamaToken token in this._tokens)
            {
                yield return token;
            }
        }

        private async Task EnsureTokens()
        {
            if (this._tokens == null)
            {
                this._tokens = new LlamaTokenCollection();

                if (this.Content != null)
                {
                    await this._tokens.Append(this.Content);
                }
            }
        }
    }
}