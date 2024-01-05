using ChieApi.Interfaces;
using LlamaApiClient;

namespace LlamaApi.Shared.Extensions
{
    public static class IEnumerableIBiasAdjustorExtensions
    {
        public static async Task AdjustNext(this IEnumerable<IBiasAdjustor> tokenTransformers, InferenceEnumerator enumerator)
        {
            foreach (IBiasAdjustor tokenTransformer in tokenTransformers)
            {
                await tokenTransformer.AdjustNext(enumerator);
            }
        }
    }
}