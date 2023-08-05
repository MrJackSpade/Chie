using ChieApi.Interfaces;
using Llama.Data.Models;
using LlamaApiClient;
using System.Diagnostics;

namespace ChieApi.TokenTransformers
{
    public class InvalidCharacterBlockingTransformer : ITokenTransformer
    {
        public async IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            await foreach (LlamaToken token in selectedTokens)
            {
                if (token.Value == "�")
                {
                    Debug.WriteLine($"Blocking token [{token.Id}]...");

                    enumerator.SetLogit(token.Id, 0, LogitBiasLifeTime.Inferrence);

                    continue;
                }

                yield return token;
            }
        }
    }
}