using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class LlamaMessage : ITokenCollection
    {
        private readonly LlamaTokenCache _cache;

        private LlamaTokenCollection? _tokens;

        public LlamaMessage(string? userName, string? content, LlamaTokenType type, LlamaTokenCache cache)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException($"'{nameof(userName)}' cannot be null or empty.", nameof(userName));
            }

            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException($"'{nameof(content)}' cannot be null or empty.", nameof(content));
            }

            if (cache is null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            this.UserName = new(userName, cache, true);
            this.Content = new(" " + content.Trim(), cache, false);
            this.Type = type;
            this._cache = cache;
        }

        public LlamaMessage(string? userName, IReadOnlyLlamaTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException($"'{nameof(userName)}' cannot be null or empty.", nameof(userName));
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (cache is null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (!content.ToString().StartsWith(" "))
            {
                throw new ArgumentException($"{nameof(LlamaMessage)} content must start with space to properly tokenize");
            }

            this.UserName = new(userName, cache, true);
            this.Content = new(content);
            this.Type = type;
            this._cache = cache;
        }

        public LlamaMessage(IReadOnlyLlamaTokenCollection userName, IReadOnlyLlamaTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
        {
            if (userName is null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.UserName = new(userName);
            this.Content = new(content);
            this.Type = type;
            this._cache = cache;
        }

        public LlamaMessage(CachedTokenCollection userName, IReadOnlyLlamaTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
        {
            if (userName is null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.UserName = userName;
            this.Content = new(content);
            this.Type = type;
            this._cache = cache;
        }

        public LlamaMessage(CachedTokenCollection userName, CachedTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
        {
            if (userName is null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.UserName = userName;
            this.Content = content;
            this.Type = type;
            this._cache = cache;
        }

        public LlamaMessage(IReadOnlyLlamaTokenCollection userName, CachedTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
        {
            if (userName is null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.UserName = new(userName);
            this.Content = content;
            this.Type = type;
            this._cache = cache;
        }

        public CachedTokenCollection Content { get; }

        public long Id { get; set; }

        public LlamaTokenType Type { get; }

        public CachedTokenCollection UserName { get; }

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await this.EnsureTokens();

            foreach (LlamaToken token in this._tokens!)
            {
                yield return token;
            }
        }

        private async Task EnsureTokens()
        {
            if (this._tokens == null)
            {
                LlamaTokenCollection tokens = new();
                await tokens.Append(_cache.Get("|", true));
                await tokens.Append(this.UserName);
                await tokens.Append(_cache.Get(":", true));
                await tokens.Append(this.Content);

                this._tokens = tokens;
            }
        }
    }
}