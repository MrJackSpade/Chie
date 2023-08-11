using Llama.Core.Extensions;
using Llama.Core.Interfaces;
using Llama.Core.Utils;
using Llama.Data.Collections;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Native;

namespace Llama.Core.Samplers.FrequencyAndPresence
{
    public class FrequencyAndPresenceSampler : ISimpleSampler
    {
        private readonly FrequencyAndPresenceSamplerSettings _settings;

        public FrequencyAndPresenceSampler(FrequencyAndPresenceSamplerSettings settings)
        {
            this._settings = settings;
        }

        public void SampleNext(SampleContext sampleContext)
        {
            LlamaTokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            LastTokens lastTokens = this.GetLastTokens(sampleTokens, this._settings.RepeatTokenPenaltyWindow);

            SamplingApi.FrequencyAndPresencePenalties(sampleContext.ContextHandle, sampleContext.Candidates, lastTokens.Ids, this._settings.FrequencyPenalty, this._settings.PresencePenalty);
        }
    }
}