using Llama.Core.Extensions;
using Llama.Core.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Native;
using System.ComponentModel.Design;

namespace Llama.Core.Samplers.Repetition
{
    public class RepetitionBlockingSampler : ISimpleSampler
    {
        private readonly RepetitionBlockingSamplerSettings _settings;

        public RepetitionBlockingSampler(RepetitionBlockingSamplerSettings settings)
        {
            this._settings = settings;
        }

        public void SampleNext(SampleContext sampleContext)
        {
            LlamaTokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            LastTokens lastTokens = this.GetLastTokens(sampleTokens, _settings.MaxRepetitions);

            if (lastTokens.Ids.Length == _settings.MaxRepetitions)
            {
                int[] distinctTokens = lastTokens.Ids.Distinct().ToArray();

                if (distinctTokens.Length == 1)
                {
                    sampleContext.Candidates.SetLogit(distinctTokens[0], float.NegativeInfinity);

                    //Has to be blocked for real real
					sampleContext.OriginalCandidates.SetLogit(distinctTokens[0], float.NegativeInfinity);

                    
				}
            }
        }
    }
}