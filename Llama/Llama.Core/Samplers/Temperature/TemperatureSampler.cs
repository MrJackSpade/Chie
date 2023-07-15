using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Native;

namespace Llama.Core.Samplers.Temperature
{
    public class TemperatureSampler : IFinalSampler
    {
        private readonly TemperatureSamplerSettings _settings;

        public TemperatureSampler(TemperatureSamplerSettings temperatureSamplerSettings)
        {
            this._settings = temperatureSamplerSettings;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            int top_k = this._settings.TopK <= 0 ? NativeApi.NVocab(sampleContext.ContextHandle) : this._settings.TopK;

            // Temperature sampling
            SamplingApi.TopK(sampleContext.ContextHandle, sampleContext.Candidates, top_k, 1);
            SamplingApi.TailFree(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.TfsZ, 1);
            SamplingApi.Typical(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.TypicalP, 1);
            SamplingApi.TopP(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.TopP, 1);
            SamplingApi.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
            return SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
        }
    }
}