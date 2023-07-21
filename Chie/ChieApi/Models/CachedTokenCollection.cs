using Llama.Data.Collections;
using Llama.Data.Models;

namespace ChieApi.Models
{
    public class CachedTokenCollection : IAsyncEnumerable<LlamaToken>
    {
        private readonly LlamaTokenCache _cache;

        private readonly bool _saveToCache;

        private Task<LlamaTokenCollection> _tokens;

        private string _value;

        public CachedTokenCollection(LlamaTokenCollection tokens)
        {
            this._tokens = Task.FromResult(tokens);
        }

        public CachedTokenCollection(string value, LlamaTokenCache cache, bool saveToCache)
        {
            this._value = value;
            this._cache = cache;
            this._saveToCache = saveToCache;
        }

        public Task<LlamaTokenCollection> Tokens
        {
            get
            {
                this._tokens ??= this._cache.Get(this.Value, this._saveToCache);

                return this._tokens;
            }
        }

        public string Value
        {
            get
            {
                this._value ??= this._tokens.ToString();

                return this._value;
            }
        }

        public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            LlamaTokenCollection toReturn = await this.Tokens;

            foreach (LlamaToken token in toReturn)
            {
                yield return token;
            }
        }
    }
}