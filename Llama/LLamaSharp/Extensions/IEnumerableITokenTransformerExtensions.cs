using Llama.Collections.Interfaces;
using Llama.Context;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;

namespace Llama.Extensions
{
    public static class IEnumerableITokenTransformerExtensions
    {
        public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, Context.LlamaContextSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, IContext context, IEnumerable<LlamaToken> selectedTokens)
        {
            IEnumerable<LlamaToken> returnTokens = selectedTokens;

            foreach (ITokenTransformer tokenTransformer in tokenTransformers)
            {
                returnTokens = tokenTransformer.TransformToken(settings, context, thisGeneration, returnTokens);
            }

            return returnTokens;
        }

        public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, Context.LlamaContextSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, IContext context, LlamaToken selectedToken) => tokenTransformers.Transform(settings, thisGeneration, context, new List<LlamaToken>() { selectedToken });
    }
}