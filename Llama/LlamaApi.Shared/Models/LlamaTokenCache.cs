using Llama.Data.Interfaces;
using System.Collections.Concurrent;

namespace ChieApi.Models
{
    public class LlamaTokenCache
    {
        private readonly ConcurrentDictionary<string, IReadOnlyLlamaTokenCollection> _cache = new();

        private readonly Func<string, Task<IReadOnlyLlamaTokenCollection>> _tokenizeFunc;

        public LlamaTokenCache(Func<string, Task<IReadOnlyLlamaTokenCollection>> tokenizeFunc)
        {
            _tokenizeFunc = tokenizeFunc;
        }

        public async Task<IReadOnlyLlamaTokenCollection> Get(string value, bool cache = true)
        {
            if (_cache.TryGetValue(value, out IReadOnlyLlamaTokenCollection? token))
            {
                return token;
            }
            else
            {
                token = await _tokenizeFunc(value);
            }

            if (cache)
            {
                _cache.TryAdd(value, token);
            }

            return token;
        }
    }
}