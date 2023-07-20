using Llama.Data.Collections;
using LlamaApiClient;
using System.Collections.Concurrent;

namespace ChieApi.Models
{
    public class LlamaTokenCache
    {
        private readonly LlamaContextClient _client;

        private readonly ConcurrentDictionary<string, LlamaTokenCollection> _cache = new();
        public LlamaTokenCache(LlamaContextClient client)
        {
            _client = client;
        }

        public async Task<LlamaTokenCollection> Get(string value, bool cache = true)
        {
            if (_cache.TryGetValue(value, out LlamaTokenCollection? token))
            {
                return token;
            }
            else
            {
                token = await _client.Tokenize(value);
            }

            if (cache)
            {
                _cache.TryAdd(value, token);
            }

            return token;
        }
    }
}
