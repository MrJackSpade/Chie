using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Native;

namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatTwoSampler : ITokenSelector
    {
        private readonly MirostatSamplerSettings _settings;

        public MirostatTwoSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            float mu = this._settings.InitialMu;
            SamplingApi.Temperature(sampleContext.Candidates, this._settings.Temperature);
            return SamplingApi.TokenMirostatV2(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tau, this._settings.Eta, ref mu);
        }
    }
}