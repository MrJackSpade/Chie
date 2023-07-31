using ChieApi.Interfaces;
using ChieApi.Models;
using Llama.Data.Models;
using LlamaApiClient;
using Loxifi.AsyncExtensions;

namespace ChieApi.TokenTransformers
{
    public class TextTruncationTransformer : ITokenTransformer
    {
        private readonly LlamaTokenCache _cache;

        private readonly string _endChars;

        private readonly int _hardmax;

        private readonly int _max;

        private readonly int _min;

        private readonly Random _random = new();

        public TextTruncationTransformer(int hardmax, int max, int min, string endChars, LlamaTokenCache cache)
        {
            this._min = min;
            this._max = max;
            this._hardmax = hardmax;
            this._endChars = endChars;
            this._cache = cache;
        }

        public async IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            string written = enumerator.Enumerated.ToString();

            if (written.Length > _hardmax)
            {
                yield return LlamaToken.EOS;
                yield break;
            }

            List<LlamaToken> tokens = await selectedTokens.ToList();

            int characterCount = written.Length;

            string? nextT = tokens.FirstOrDefault()?.ToString();

            if (nextT == null)
            {
                await foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }

                yield break;
            }

            float chance = (characterCount - this._min) / (float)(this._max - this._min);

            bool truncate = this._random.NextDouble() < chance;

            if (!truncate || !this.GoodEndChar(written) || !nextT.StartsWith(" "))
            {
                await foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }

                yield break;
            }

            yield return LlamaToken.EOS;
        }

        private bool GoodEndChar(string toTest)
        {
            string t = toTest.Trim();

            foreach (char c in this._endChars)
            {
                if (t.EndsWith(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}