using Llama.Context.Samplers.Interfaces;
using Llama.Native;
using Llama.Services;

namespace Llama.Context.Samplers.Temperature
{
    public class GreedySampler : IFinalSampler
    {
        private readonly int _newLineId;

        public GreedySampler()
        {
            this._newLineId = NativeApi.TokenNl();
        }

        public int SampleNext(SampleContext sampleContext)
        {
            int id;
            do
            {
                id = SamplingService.TokenGreedy(sampleContext.ContextHandle, sampleContext.Candidates);
            } while (sampleContext.InferrenceTokens.Count == 0 && id == this._newLineId);

            return id;
        }
    }
}