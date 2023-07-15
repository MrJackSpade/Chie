using Llama.Data.Interfaces;
using Llama.Data.Native;

namespace Llama.Data.Models
{
    public struct SampleContext
    {
        public LlamaTokenDataArray Candidates { get; set; }

        public SafeLlamaContextHandle ContextHandle { get; set; }

        public IReadOnlyLlamaTokenCollection ContextTokens { get; set; }

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