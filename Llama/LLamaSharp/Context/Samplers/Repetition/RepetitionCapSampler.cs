using Llama.Context.Samplers.Interfaces;
using Llama.Data;
using Llama.Native.Data;
using System;
using System.Linq;

namespace Llama.Context.Samplers.Repetition
{
    public class RepetitionCapSampler : ISimpleSampler
    {
        private readonly int _maxRepetitionCap;

        public RepetitionCapSampler(int maxRepetitionCap)
        {
            this._maxRepetitionCap = maxRepetitionCap;
        }

        public void SampleNext(SampleContext sampleContext)
        {
            if (sampleContext.InferrenceTokens.Count < _maxRepetitionCap)
            {
                return;
            }

            int tokenCount = sampleContext.InferrenceTokens.Count;

            LlamaToken[] lastN = sampleContext.InferrenceTokens.Skip(tokenCount - _maxRepetitionCap).Distinct().ToArray();

            if (lastN.Length == 1)
            {
                Span<LlamaTokenData> span = sampleContext.Candidates.data.Span;
                LlamaTokenData existing = span[lastN[0].Id];
                span[lastN[0].Id] = new LlamaTokenData()
                {
                    id = existing.id,
                    logit = float.NegativeInfinity,
                    p = float.NegativeInfinity
                };
            }
        }
    }
}