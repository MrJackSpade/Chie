using Llama.Data.Collections;
using Llama.Data.Models;
using Ai.Utils.Extensions;
using Llama.Data.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;

namespace ChieApi.Samplers
{
    public class NewlineEnsureSampler : ISimpleSampler
    {
        readonly LlamaTokenCache _tokenCache;
        public NewlineEnsureSampler(LlamaTokenCache cache)
        {
            _tokenCache = cache;
        }
        public async Task<Dictionary<int, float>> SampleNext(LlamaTokenCollection thisInferrence)
        {
            Dictionary<int, float> toReturn = new()
            {
                { 30004, 0 }
            };

            LlamaToken? lastToken = thisInferrence.LastOrDefault();

            if (lastToken is not null && lastToken.Id == 13)
            {
                toReturn.AddOrUpdate(13, 0);
            }

            LlamaToken[] banTokens = new LlamaToken[]
            {
                (await _tokenCache.Get("|")).Single(),
                (await _tokenCache.Get(" |")).Single(),
            };

            foreach (LlamaToken t in banTokens)
            {
                toReturn.AddOrUpdate(t.Id, 0);
            }

            return toReturn;
        }
    }
}
