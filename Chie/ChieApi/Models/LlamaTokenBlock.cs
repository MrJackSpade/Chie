using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class LlamaTokenBlock : ITokenCollection
    {
        private readonly LlamaTokenCache _cache;

        private LlamaTokenCollection _cachedContent;

        private LlamaTokenCollection _tokens;

        public LlamaTokenBlock()
        {
            this.Content = null;
            this._cache = null;
            this.Type = LlamaTokenType.Undefined;
        }

        public LlamaTokenBlock(string content, LlamaTokenType type, LlamaTokenCache cache)
        {
            this.Content = content;
            this._cache = cache;
            this.Type = type;
        }

        public LlamaTokenBlock(LlamaTokenCollection content)
        {
            this.Content = content.ToString();
            this._cachedContent = content;
        }

        public string Content { get; private set; }

        public LlamaTokenType Type { get; private set; }

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
                if (!string.IsNullOrEmpty(this.Content))
                {
                    this._cachedContent ??= await this._cache.Get(this.Content, false);

                    this._tokens = this._cachedContent;
                } else
                {
                    this._tokens = new LlamaTokenCollection();
                }
            }
        }
    }
}