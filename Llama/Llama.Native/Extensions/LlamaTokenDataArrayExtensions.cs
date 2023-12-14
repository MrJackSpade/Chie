using Llama.Data.Native;

namespace Llama.Native.Extensions
{
    public static class LlamaTokenDataArrayExtensions
    {
        public static void SetLogit(this LlamaTokenDataArray candidates, int tokenId, float logit)
        {
            Span<LlamaTokenData> span = candidates.Data.Span;
            int index = GetTokenIndex(candidates, tokenId);

            LlamaTokenData existing = span[index];
            span[index] = new LlamaTokenData()
            {
                id = existing.id,
                logit = logit,
                p = logit
            };
        }

        private static int GetTokenIndex(this LlamaTokenDataArray candidates, int tokenId)
        {
            for (int i = 0; i < candidates.Data.Span.Length; i++)
            {
                if (candidates.Data.Span[i].id == tokenId)
                {
                    return i;
                }
            }

            throw new KeyNotFoundException();
        }
    }
}