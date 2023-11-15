using Llama.Data.Models;
using Llama.Data.Native;

namespace Llama.Data.Extensions
{
    public static class SampleContextExtensions
    {
        public static float GetProbability(this SampleContext context, int tokenId)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            LlamaTokenData existing = span[tokenId];
            return existing.logit;
        }

        public static void SetBias(this SampleContext context, int tokenId, float probability, LogitBiasType logitBiasType)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            LlamaTokenData existing = span[tokenId];

            int mod = existing.logit > 0 ? 1 : -1;

            span[tokenId] = logitBiasType switch
            {
                LogitBiasType.Additive => new LlamaTokenData()
                {
                    id = existing.id,
                    logit = existing.logit + probability,
                    p = existing.p + probability
                },
                LogitBiasType.Multiplicative => new LlamaTokenData()
                {
                    id = existing.id,
                    logit = existing.logit * probability * mod,
                    p = existing.p * probability * mod
                },
                _ => throw new NotImplementedException(),
            };
        }

        public static void SetPenalty(this SampleContext context, int tokenId, float probability)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            LlamaTokenData existing = span[tokenId];

            float newValue = existing.logit / probability;

            if (existing.logit <= 0)
            {
                newValue = existing.logit * probability;
            }

            span[tokenId] = new LlamaTokenData()
            {
                id = existing.id,
                logit = newValue,
                p = newValue
            };
        }

        public static void SetProbability(this SampleContext context, int tokenId, float probability)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            LlamaTokenData existing = span[tokenId];
            span[tokenId] = new LlamaTokenData()
            {
                id = existing.id,
                logit = probability,
                p = probability
            };
        }

        public static void Update(this SampleContext context, IEnumerable<KeyValuePair<LlamaToken, float>> list)
        {
            foreach (KeyValuePair<LlamaToken, float> llamaToken in list)
            {
                context.SetProbability(llamaToken.Key.Id, llamaToken.Value);
            }
        }
    }
}