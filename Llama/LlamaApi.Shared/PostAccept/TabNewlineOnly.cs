using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class TabNewlineOnly : IPostAccept
    {
        public TabNewlineOnly()
        {
        }

        public async Task PostAccept(InferenceEnumerator enumerator)
        {
            if (enumerator.Enumerated.Count == 0)
            {
                return;
            }

            if (enumerator.Enumerated.Last().Id != 13)
            {
                enumerator.SetBias(12, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
        }
    }
}