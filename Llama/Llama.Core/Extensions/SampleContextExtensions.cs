﻿using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;

namespace Llama.Core.Extensions
{
    public static class SampleContextExtensions
    {
        public static LlamaTokenData GetData(this SampleContext sampleContext, int tokenId)
        {
            LlamaTokenData[] candidates = sampleContext.Candidates.Data.Span.ToArray();

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].id == tokenId)
                {
                    return candidates[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

        public static string GetDisplayString(this SampleContext ctx, int tokenId)
        {
            LlamaTokenData tokenData = new();

            for (ulong i = 0; i < ctx.OriginalCandidates.Size; i++)
            {
                if (ctx.OriginalCandidates[i].id == tokenId)
                {
                    tokenData = ctx.OriginalCandidates[i];
                    break;
                }
            }

            LlamaTokenData newTokenData = new();

            for (int i = 0; i < ctx.Candidates.Data.Length; i++)
            {
                if (ctx.Candidates.Data.Span[i].id == tokenId)
                {
                    newTokenData = ctx.Candidates.Data.Span[i];
                    break;
                }
            }

            LlamaToken token = ctx.GetToken(tokenData.id);

            return $"{token.GetEscapedValue()} ({tokenData.p:0.00} => {newTokenData.p:0.00})";
        }

        public static LlamaTokenData GetOriginalData(this SampleContext sampleContext, int tokenId)
        {
            LlamaTokenDataArray candidates = sampleContext.OriginalCandidates;

            for (ulong i = 0; i < candidates.Size; i++)
            {
                if (candidates[i].id == tokenId)
                {
                    return candidates[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

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

        public static LlamaToken GetToken(this SampleContext ctx, int id)
        {
            return new(id, NativeApi.TokenToPiece(ctx.ModelHandle, id));
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

            tokens.Sorted = false;
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

            tokens.Sorted = false;
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

            tokens.Sorted = false;
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

            tokens.Sorted = false;
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

            tokens.Sorted = false;
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