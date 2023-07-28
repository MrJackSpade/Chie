using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class CachedTokenCollection : IAsyncEnumerable<LlamaToken>
    {
        private readonly LlamaTokenCache _cache;

        private readonly bool _saveToCache;

        private Task<IReadOnlyLlamaTokenCollection> _tokens;

        private string? _value;

        public CachedTokenCollection(IReadOnlyLlamaTokenCollection tokens)
        {
            this._tokens = Task.FromResult(tokens);
        }

        public CachedTokenCollection(string value, LlamaTokenCache cache, bool saveToCache)
        {
            this._value = value ?? throw new ArgumentNullException(nameof(value));
            this._cache = cache;
            this._saveToCache = saveToCache;
        }

        public Task<IReadOnlyLlamaTokenCollection> Tokens
        {
            get
            {
                this._tokens ??= this._cache.Get(this._value, this._saveToCache);

                return this._tokens;
            }
        }

        private async Task<string?> GetValue()
        {
            if (this._value == null)
            {
                IReadOnlyLlamaTokenCollection tc = await this.Tokens;

                this._value = tc?.ToString();
            }

            return this._value;
        }

        public Task<string?> Value => this.GetValue();

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            IReadOnlyLlamaTokenCollection toReturn = await this.Tokens;

            foreach (LlamaToken token in toReturn)
            {
                yield return token;
            }
        }
    }
}