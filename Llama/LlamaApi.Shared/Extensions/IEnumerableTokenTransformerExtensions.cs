using ChieApi.Interfaces;
using Llama.Data.Models;
using LlamaApiClient;
using Loxifi.AsyncExtensions;

namespace LlamaApi.Shared.Extensions
{
    public static class IEnumerableITokenTransformerExtensions
    {
        public static IAsyncEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            IAsyncEnumerable<LlamaToken> returnTokens = selectedTokens;

            foreach (ITokenTransformer tokenTransformer in tokenTransformers)
            {
                returnTokens = tokenTransformer.TransformToken(enumerator, returnTokens);
            }

            return returnTokens;
        }

        public static IAsyncEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, InferenceEnumerator enumerator, LlamaToken selectedToken) => tokenTransformers.Transform(enumerator, new List<LlamaToken>() { selectedToken }.ToAsyncEnumerable());
    }
}
