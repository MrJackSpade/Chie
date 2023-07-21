using ChieApi.Interfaces;
using global::Llama.Data.Interfaces;
using global::Llama.Data.Models;
using Llama.Data.Extensions;

namespace ChieApi.Extensions
{
    namespace Llama.Extensions
    {
        public static class IEnumerableISimpleSampler
        {
            public static async Task<Dictionary<int, float>> SampleNext(this IEnumerable<ISimpleSampler> tokenTransformers, IReadOnlyLlamaTokenCollection thisCall)
            {
                Dictionary<int, float> returnTokens = new();

                foreach (ISimpleSampler tokenTransformer in tokenTransformers)
                {
                    Dictionary<int, float> thisLogits = await tokenTransformer.SampleNext(thisCall);

                    returnTokens.AddOrUpdate(thisLogits);
                }

                return returnTokens;
            }
        }
    }
}
