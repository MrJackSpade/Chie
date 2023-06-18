using Llama.Data;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;

namespace Llama.Extensions
{
    public static class IEnumerableIPostResponseContextTransformerExtensions
    {
        public static IEnumerable<LlamaToken> Transform(this IEnumerable<IPostResponseContextTransformer> tokenTransformers, IEnumerable<LlamaToken> buffer)
        {
            IEnumerable<LlamaToken> returnTokens = buffer;

            foreach (IPostResponseContextTransformer tokenTransformer in tokenTransformers)
            {
                returnTokens = tokenTransformer.Transform(returnTokens);
            }

            return returnTokens;
        }
    }
}