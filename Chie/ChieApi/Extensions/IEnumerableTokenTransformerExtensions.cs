using ChieApi.Interfaces;
using global::Llama.Data.Interfaces;
using global::Llama.Data.Models;

namespace ChieApi.Extensions
{
    namespace Llama.Extensions
    {
        public static class IEnumerableITokenTransformerExtensions
        {
            public static IAsyncEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, IReadOnlyLlamaTokenCollection thisCall, IAsyncEnumerable<LlamaToken> selectedTokens)
            {
                IAsyncEnumerable<LlamaToken> returnTokens = selectedTokens;

                foreach (ITokenTransformer tokenTransformer in tokenTransformers)
                {
                    returnTokens = tokenTransformer.TransformToken(thisCall, selectedTokens);
                }

                return returnTokens;
            }

            public static IAsyncEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, IReadOnlyLlamaTokenCollection thisCall, LlamaToken selectedToken) => tokenTransformers.Transform(thisCall, new List<LlamaToken>() { selectedToken }.ToAsyncEnumerable());
        }
    }
}
