using Llama.Data.Interfaces;
using LlamaApiClient;
using System.Collections.Concurrent;

namespace ChieApi.Models
{
    public class LlamaTokenCache
    {
        private readonly ConcurrentDictionary<string, IReadOnlyLlamaTokenCollection> _cache = new();

        private readonly LlamaContextClient _client;

        public LlamaTokenCache(LlamaContextClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyLlamaTokenCollection> Get(string value, bool cache = true)
        {
            if (_cache.TryGetValue(value, out IReadOnlyLlamaTokenCollection? token))
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