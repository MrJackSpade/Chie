using ChieApi.Interfaces;
using ChieApi.Models;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.Samplers
{
    public class NewlineEnsureSampler : ISimpleSampler
    {
        private readonly LlamaTokenCache _tokenCache;

        public NewlineEnsureSampler(LlamaTokenCache cache)
        {
            this._tokenCache = cache;
        }

        public async Task SampleNext(InferenceEnumerator enumerator)
        {
            enumerator.SetLogit(30004, 0, LogitBiasLifeTime.Inferrence);

            LlamaToken? lastToken = enumerator.Enumerated.LastOrDefault();

            if (lastToken is not null && lastToken.Id == 13)
            {
                enumerator.SetLogit(13, 0, LogitBiasLifeTime.Temporary);
            }

            LlamaToken[] banTokens = new LlamaToken[]
            {
                (await this._tokenCache.Get("|")).Single(),
                (await this._tokenCache.Get(" |")).Single(),
            };

            foreach (LlamaToken t in banTokens)
            {
                enumerator.SetLogit(t.Id, 0, LogitBiasLifeTime.Inferrence);
            }
        }
    }
}