using Llama.Data.Models;
using Llama.Data.Native;

namespace Llama.Core.Extensions
{
    public static class SampleContextExtensions
    {
        public static float GetOriginalProbability(this SampleContext context, int tokenId)
        {
            foreach (LlamaTokenData ltd in context.OriginalCandidates)
            {
                if (ltd.id == tokenId)
                {
                    return ltd.p;
                }
            }

            throw new InvalidDataException();
        }

        public static float GetProbability(this SampleContext context, int tokenId)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            int index = GetTokenIndex(context, tokenId);
            LlamaTokenData existing = span[index];
            return existing.logit;
        }

        public static void SetBias(this SampleContext context, int tokenId, float probability, LogitBiasType logitBiasType)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            int index = GetTokenIndex(context, tokenId);

            LlamaTokenData existing = span[index];

            int mod = existing.logit > 0 ? 1 : -1;

            span[index] = logitBiasType switch
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
            int index = GetTokenIndex(context, tokenId);

            LlamaTokenData existing = span[index];

            float newValue = existing.logit / probability;

            if (existing.logit <= 0)
            {
                newValue = existing.logit * probability;
            }

            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = newValue,
                p = newValue
            };
        }

        public static void SetProbability(this SampleContext context, int tokenId, float probability)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            int index = GetTokenIndex(context, tokenId);

            LlamaTokenData existing = span[index];
            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = probability,
                p = probability
            };
        }

        public static void SetLogit(this SampleContext context, int tokenId, float logit)
        {
            Span<LlamaTokenData> span = context.Candidates.Data.Span;
            int index = GetTokenIndex(context, tokenId);

            LlamaTokenData existing = span[index];
            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = logit,
                p = logit
            };
        }

        public static void Update(this SampleContext context, IEnumerable<KeyValuePair<LlamaToken, float>> list)
        {
            foreach (KeyValuePair<LlamaToken, float> llamaToken in list)
            {
                context.SetProbability(llamaToken.Key.Id, llamaToken.Value);
            }
        }

        private static int GetTokenIndex(this SampleContext context, int tokenId)
        {
            for (int i = 0; i < context.Candidates.Data.Span.Length; i++)
            {
                if (context.Candidates.Data.Span[i].id == tokenId)
                {
                    return i;
                }
            }

            throw new KeyNotFoundException();
        }
    }
}