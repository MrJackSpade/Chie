using Llama.Core.Utils;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
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

            int repeat_last_n = this._settings.RepeatTokenPenaltyWindow < 0 ? sampleTokens.Count : this._settings.RepeatTokenPenaltyWindow;

            int[] ids = sampleTokens.Ids.ToArray();

            ulong last_n_repeat = (ulong)LlamaMath.Min(repeat_last_n, sampleTokens.Count, ids.Length);

            SamplingApi.FrequencyAndPresencePenalties(sampleContext.ContextHandle, sampleContext.Candidates, ids, last_n_repeat, this._settings.FrequencyPenalty, this._settings.PresencePenalty);
        }
    }
}