using Llama.Core.Extensions;
using Llama.Core.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Native;

namespace Llama.Core.Samplers.Repetition
{
    public class RepetitionSampler : ISimpleSampler
    {
        private readonly RepetitionSamplerSettings _settings;

        private readonly HashSet<int> _exclude = new();

        public RepetitionSampler(RepetitionSamplerSettings settings)
        {
            this._settings = settings;

            foreach(int i in settings.Exclude)
            {
                _exclude.Add(i);
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            LlamaTokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            LastTokens lastTokens = this.GetLastTokens(sampleTokens, this._settings.RepeatPenaltyWindow, _exclude);

            SamplingApi.RepetitionPenalties(sampleContext.Candidates, lastTokens.Ids, this._settings.RepeatPenalty, _settings.FrequencyPenalty, _settings.PresencePenalty);
        }
    }
}