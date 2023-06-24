using Llama.Context.Samplers.Interfaces;
using Llama.Services;

namespace Llama.Context.Samplers.Mirostat
{
    public class MirostatTwoSampler : IFinalSampler
    {
        private readonly MirostatSamplerSettings _settings;

        public MirostatTwoSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            float mu = this._settings.InitialMu;
            SamplingService.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
            return SamplingService.TokenMirostatV2(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tau, this._settings.Eta, ref mu);
        }
    }
}