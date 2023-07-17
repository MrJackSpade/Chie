using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Native;

namespace Llama.Core.Samplers.Temperature
{
    public class GreedySampler : ITokenSelector
    {
        public GreedySampler()
        {
        }

        public int SampleNext(SampleContext sampleContext) => SamplingApi.TokenGreedy(sampleContext.ContextHandle, sampleContext.Candidates);
    }
}