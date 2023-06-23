using Llama.Context.Samplers.Interfaces;
using Llama.Native;
using Llama.Services;

namespace Llama.Context.Samplers.Temperature
{
    public class TemperatureSampler : IFinalSampler
    {
        private readonly int _newLineId;

        private readonly TemperatureSamplerSettings _settings;

        public TemperatureSampler(TemperatureSamplerSettings temperatureSamplerSettings)
        {
            this._settings = temperatureSamplerSettings;
            this._newLineId = NativeApi.llama_token_nl();
        }

        public int SampleNext(SampleContext sampleContext)
        {
            int top_k = this._settings.TopK <= 0 ? NativeApi.llama_n_vocab(sampleContext.ContextHandle) : this._settings.TopK;

            int id;
            do
            {
                // Temperature sampling
                SamplingService.TopK(sampleContext.ContextHandle, sampleContext.Candidates, top_k, 1);
                SamplingService.TailFree(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.TfsZ, 1);
                SamplingService.Typical(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.TypicalP, 1);
                SamplingService.TopP(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.TopP, 1);
                SamplingService.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);
                id = SamplingService.Token(sampleContext.ContextHandle, sampleContext.Candidates);
            } while (sampleContext.InferrenceTokens.Count == 0 && id == this._newLineId);

            return id;
        }
    }
}