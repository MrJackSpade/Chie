using ChieApi.Interfaces;
using ChieApi.Models;
using Llama.Data.Extensions;
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
            enumerator.SetBias(30004, float.NegativeInfinity, LogitRuleLifetime.Inferrence);

            LlamaToken? lastToken = enumerator.Enumerated.LastOrDefault();

            if (lastToken is not null && lastToken.Id == 13)
            {
                enumerator.SetBias(13, float.NegativeInfinity, LogitRuleLifetime.Token);
            }

            LlamaToken[] banTokens = new LlamaToken[]
            {
                (await this._tokenCache.Get("|")).Single(),
                (await this._tokenCache.Get(" |")).Single(),
            };

            foreach (LlamaToken t in banTokens)
            {
                enumerator.SetBias(t.Id, float.NegativeInfinity, LogitRuleLifetime.Inferrence);
            }
        }
    }
}