using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class LlamaMessage : ITokenCollection
    {
        private readonly LlamaTokenCache _cache;

        private LlamaTokenCollection? _cachedContent;

        private LlamaTokenCollection _tokens;

        public LlamaMessage(string userName, string content, LlamaTokenType type, LlamaTokenCache cache)
        {
            this.UserName = userName;
            this.Content = content;
            this.Type = type;
            this._cache = cache;
        }

        public LlamaMessage(string userName, LlamaTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
        {
            this.UserName = userName;
            this.Content = content.ToString();
            this._cachedContent = content;
            this.Type = type;
            this._cache = cache;
        }

        public string Content { get; }

        public LlamaTokenType Type { get; }

        public string UserName { get; }

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
                LlamaTokenCollection tokens = new();

                LlamaTokenCollection userTokens = await this._cache.Get($"|{this.UserName}:");
                this._cachedContent ??= await this._cache.Get(" " + this.Content, false);

                tokens.Append(userTokens);
                tokens.Append(this._cachedContent);

                this._tokens = tokens;
            }
        }
    }
}