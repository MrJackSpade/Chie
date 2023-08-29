using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;
using System.Diagnostics;

namespace ChieApi.TokenTransformers
{
    public class SpaceStartTransformer : ITokenTransformer
    {
        public async IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            if (enumerator.Enumerated.Count > 0)
            {
                await foreach (LlamaToken t in selectedTokens)
                {
                    yield return t;
                }

                yield break;
            }

            await foreach (LlamaToken token in selectedTokens)
            {
                string? tstring = token?.ToString();

                if (tstring != null && tstring.StartsWith(" "))
                {
                    yield return token;
                }
                else
                {
                    Debug.WriteLine($"Skipping: {token}");
                    enumerator.SetBias(token.Id, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
                }
            }
        }
    }
}