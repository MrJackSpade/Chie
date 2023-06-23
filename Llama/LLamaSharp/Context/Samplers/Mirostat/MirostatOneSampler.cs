using Llama.Context.Samplers.Interfaces;
using Llama.Services;

namespace Llama.Context.Samplers.Mirostat
{
    public class MirostatOneSampler : IFinalSampler
    {
        private readonly MirostatSamplerSettings _settings;

        private float _mu;

        public MirostatOneSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
            this._mu = this._settings.InitialMu;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            SamplingService.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
            return SamplingService.TokenMirostat(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tau, this._settings.Eta, this._settings.M, ref this._mu);
        }
    }
}