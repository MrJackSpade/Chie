using ChieApi.Interfaces;
using ChieApi.Models;
using Llama.Data.Extensions;
using Loxifi.AsyncExtensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.TokenTransformers
{
    public class TextTruncationTransformer : ITokenTransformer
    {
        private readonly string _endChars;

        private readonly int _max;

        private readonly int _min;

        private readonly Random _random = new();

        readonly LlamaTokenCache _cache;

        public TextTruncationTransformer(int max, int min, string endChars, LlamaTokenCache cache)
        {
            this._min = min;
            this._max = max;
            this._endChars = endChars;
            this._cache = cache;
        }

        public async IAsyncEnumerable<LlamaToken> TransformToken(IReadOnlyLlamaTokenCollection thisCall, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            List<LlamaToken> tokens = await selectedTokens.ToList();

            string written = thisCall.ToString();

            int characterCount = written.Length;

            string? nextT = tokens.FirstOrDefault()?.ToString();

            if (nextT == null)
            {
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

            yield return (await this._cache.Get("\n")).Single();
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