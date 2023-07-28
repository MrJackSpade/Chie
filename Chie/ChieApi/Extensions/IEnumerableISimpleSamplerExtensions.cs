using ChieApi.Interfaces;
using LlamaApiClient;

namespace ChieApi.Extensions
{
    namespace Llama.Extensions
    {
        public static class IEnumerableISimpleSampler
        {
            public static async Task SampleNext(this IEnumerable<ISimpleSampler> tokenTransformers, InferenceEnumerator enumerator)
            {
                foreach (ISimpleSampler tokenTransformer in tokenTransformers)
                {
                    await tokenTransformer.SampleNext(enumerator);
                }
            }
        }
    }
}