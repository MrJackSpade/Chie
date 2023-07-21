using Llama.Core.Interfaces;
using Llama.Core.Utils;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Native;

namespace Llama.Core.Samplers.Repetition
{
    public class RepetitionSampler : ISimpleSampler
    {
        private readonly RepetitionSamplerSettings _settings;

        public RepetitionSampler(RepetitionSamplerSettings settings)
        {
            this._settings = settings;
        }

        public void SampleNext(SampleContext sampleContext)
        {
            LlamaTokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            int repeat_last_n = this._settings.RepeatTokenPenaltyWindow < 0 ? sampleTokens.Count : this._settings.RepeatTokenPenaltyWindow;

            int[] ids = sampleTokens.Ids.ToArray();

            ulong last_n_repeat = (ulong)LlamaMath.Min(repeat_last_n, sampleTokens.Count, ids.Length);

            SamplingApi.RepetitionPenalty(sampleContext.ContextHandle, sampleContext.Candidates, ids, last_n_repeat, this._settings.RepeatPenalty);
        }
    }
}