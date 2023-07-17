using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Native;

namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatOneSampler : ITokenSelector
    {
        private readonly MirostatSamplerSettings _settings;

        public MirostatOneSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            float mu = this._settings.InitialMu;
            SamplingApi.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
            return SamplingApi.TokenMirostat(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tau, this._settings.Eta, this._settings.M, ref mu);
        }
    }
}