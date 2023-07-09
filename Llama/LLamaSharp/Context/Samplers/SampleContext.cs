using Llama.Collections.Interfaces;
using Llama.Native;
using Llama.Native.Data;
using System;

namespace Llama.Context.Samplers
{
    public struct SampleContext
    {
        public LlamaTokenDataArray Candidates { get; set; }

        public SafeLlamaContextHandle ContextHandle { get; set; }

        public IReadOnlyLlamaTokenCollection ContextTokens { get; set; }

        public IReadOnlyLlamaTokenCollection InferrenceTokens { get; set; }

        public void SetProbability(int tokenId, float probability)
        {
            Span<LlamaTokenData> span = this.Candidates.data.Span;
            LlamaTokenData existing = span[tokenId];
            span[tokenId] = new LlamaTokenData()
            {
                id = existing.id,
                logit = probability,
                p = probability
            };
        }
    }
}