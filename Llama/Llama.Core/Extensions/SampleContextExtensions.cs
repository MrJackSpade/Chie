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

        public static float GetProbability(this LlamaTokenDataArray tokens, int tokenId)
        {
            Span<LlamaTokenData> span = tokens.Data.Span;
            int index = GetTokenIndex(tokens, tokenId);
            LlamaTokenData existing = span[index];
            return existing.logit;
        }

        public static void SetBias(this LlamaTokenDataArray tokens, int tokenId, float probability, LogitBiasType logitBiasType)
        {
            Span<LlamaTokenData> span = tokens.Data.Span;

            int index = GetTokenIndex(tokens, tokenId);

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

        public static void SetLogit(this LlamaTokenDataArray tokens, int tokenId, float logit)
        {
            Span<LlamaTokenData> span = tokens.Data.Span;
            int index = GetTokenIndex(tokens, tokenId);

            LlamaTokenData existing = span[index];
            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = logit,
                p = logit
            };
        }

        public static void SetLogitAtIndex(this LlamaTokenDataArray tokens, int index, float logit)
        {
            Span<LlamaTokenData> span = tokens.Data.Span;
            LlamaTokenData existing = span[index];
            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = logit,
                p = logit
            };
        }

        public static void SetPenalty(this LlamaTokenDataArray tokens, int tokenId, float probability)
        {
            Span<LlamaTokenData> span = tokens.Data.Span;
            int index = GetTokenIndex(tokens, tokenId);

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

        public static void SetProbability(this LlamaTokenDataArray tokens, int tokenId, float probability)
        {
            Span<LlamaTokenData> span = tokens.Data.Span;
            int index = GetTokenIndex(tokens, tokenId);

            LlamaTokenData existing = span[index];
            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = probability,
                p = probability
            };
        }

        public static void Update(this LlamaTokenDataArray tokens, IEnumerable<KeyValuePair<LlamaToken, float>> list)
        {
            foreach (KeyValuePair<LlamaToken, float> llamaToken in list)
            {
                tokens.SetProbability(llamaToken.Key.Id, llamaToken.Value);
            }
        }

        private static int GetTokenIndex(this LlamaTokenDataArray tokens, int tokenId)
        {
            for (int i = 0; i < tokens.Data.Span.Length; i++)
            {
                if (tokens.Data.Span[i].id == tokenId)
                {
                    return i;
                }
            }

            throw new KeyNotFoundException();
        }
    }
}