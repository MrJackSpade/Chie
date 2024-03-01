using Llama.Core.Extensions;
using Llama.Core.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Data.Native;
using Llama.Native;

namespace Llama.Core.Samplers.Temperature
{
    public class MinPSampler : ISimpleSampler
    {
        private readonly MinPSamplerSettings _settings;

        public MinPSampler(MinPSamplerSettings temperatureSamplerSettings)
        {
            this._settings = temperatureSamplerSettings;
        }

        public void ApplyOriginalMinP(SampleContext context)
        {
            Dictionary<int, int> mapping = new();

            Span<LlamaTokenData> newData = context.Candidates.Data.Span;

            for (int i = 0; i < context.Candidates.Data.Length; i++)
            {
                LlamaTokenData newToken = newData[i];
                mapping.Add(newToken.id, i);
            }

            foreach (LlamaTokenData token in context.OriginalCandidates)
            {
                float minp = this._settings.MinP;

                if (_settings.MinPs.TryGetValue(token.id, out float cminp))
                {
                    minp = Math.Max(minp, cminp);
                }

                if (token.p < minp)
                {
                    int newIndex = mapping[token.id];
                    context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                }
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);
            this.ApplyOriginalMinP(sampleContext);
            SamplingApi.SoftMax(sampleContext.Candidates);

            SamplingApi.MinP(sampleContext.Candidates, this._settings.MinP);
        }
    }
}