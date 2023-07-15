using Llama.Collections.Interfaces;
using Llama.Context;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace Llama.Pipeline.TokenTransformers
{
    public class InvalidCharacterBlockingTransformer : ITokenTransformer
    {
        public IEnumerable<LlamaToken> TransformToken(Context.LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisGeneration, IEnumerable<LlamaToken> selectedTokens)
        {
            foreach (LlamaToken token in selectedTokens)
            {
                if (token.Value == "�")
                {
                    Debug.WriteLine($"Blocking token [{token.Id}]...");

                    if (!settings.LogitBias.ContainsKey(token.Id))
                    {
                        settings.LogitBias.Add(token.Id, float.NegativeInfinity);
                    }

                    continue;
                }

                yield return token;
            }
        }
    }
}