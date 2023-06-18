using Llama.Collections;
using Llama.Context.Samplers.Interfaces;
using Llama.Services;
using Llama.Utilities.Utilities;
using System.Linq;

namespace Llama.Context.Samplers.FrequencyAndPresence
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

            int repeat_last_n = this._settings.RepeatTokenPenaltyWindow < 0 ? sampleTokens.Count : this._settings.RepeatTokenPenaltyWindow;

            int[] ids = sampleTokens.Ids.ToArray();

            ulong last_n_repeat = (ulong)LlamaMath.Min(repeat_last_n, sampleTokens.Count, ids.Length);

            SamplingService.FrequencyAndPresencePenalties(sampleContext.ContextHandle, sampleContext.Candidates, ids, last_n_repeat, this._settings.FrequencyPenalty, this._settings.PresencePenalty);
        }
    }
}