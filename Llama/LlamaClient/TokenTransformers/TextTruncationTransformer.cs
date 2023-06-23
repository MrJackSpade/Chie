using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Pipeline.Interfaces;

namespace Llama.TokenTransformers
{
    public class TextTruncationTransformer : ITokenTransformer
    {
        private readonly int _max;
        private readonly int _min;
        private readonly string _endChars;
        public TextTruncationTransformer(int max, int min, string endChars)
        {
            this._min = min;
            this._max = max;
            this._endChars = endChars;
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
        private readonly Random _random = new();

        public IEnumerable<LlamaToken> TransformToken(LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisCall, IEnumerable<LlamaToken> selectedTokens)
        {
            List<LlamaToken> tokens = selectedTokens.ToList();

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
                foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }

                yield break;
            }

            yield return context.GetToken(13, LlamaTokenTags.RESPONSE);
        }
    }
}
