using Llama.Context.Samplers.Interfaces;
using Llama.Services;

namespace Llama.Context.Samplers.Mirostat
{
    public class MirostatOneSampler : IFinalSampler
    {
        private readonly MirostatSamplerSettings _settings;

        public MirostatOneSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            float mu = this._settings.InitialMu;
            SamplingService.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
            return SamplingService.TokenMirostat(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tau, this._settings.Eta, this._settings.M, ref mu);
        }
    }
}