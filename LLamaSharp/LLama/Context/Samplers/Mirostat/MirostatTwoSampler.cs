using Llama.Context.Samplers.Interfaces;
using Llama.Services;

namespace Llama.Context.Samplers.Mirostat
{
    public class MirostatTwoSampler : IFinalSampler
    {
        private readonly MirostatSamplerSettings _settings;

        private float _mu;

        public MirostatTwoSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
            this._mu = this._settings.InitialMu;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            SamplingService.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
            return SamplingService.TokenMirostatV2(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tau, this._settings.Eta, ref this._mu);
        }
    }
}