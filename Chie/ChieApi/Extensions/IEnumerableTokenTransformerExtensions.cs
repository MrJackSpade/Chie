using ChieApi.Interfaces;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Loxifi.AsyncExtensions;

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
