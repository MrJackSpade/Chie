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

        private readonly SpecialTokens _specialTokens;

        public NewlineEnsureSampler(LlamaTokenCache cache, SpecialTokens specialTokens)
        {
            this._tokenCache = cache;
            this._specialTokens = specialTokens;
        }

        public async Task SampleNext(InferenceEnumerator enumerator)
        {

            enumerator.SetBias(30004, float.NegativeInfinity, LogitRuleLifetime.Inferrence, LogitBiasType.Additive);

            LlamaToken? lastToken = enumerator.Enumerated.LastOrDefault();

            if (lastToken is not null && lastToken.Id == _specialTokens.NewLine)
            {
                enumerator.SetBias(_specialTokens.NewLine, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }

            LlamaToken[] banTokens = new LlamaToken[]
            {
                (await this._tokenCache.Get("|")).Single(),
                (await this._tokenCache.Get(" |")).Single(),
            };

            foreach (LlamaToken t in banTokens)
            {
                enumerator.SetBias(t.Id, float.NegativeInfinity, LogitRuleLifetime.Inferrence, LogitBiasType.Additive);
            }
        }
    }
}