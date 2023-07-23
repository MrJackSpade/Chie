using Llama.Data.Models;

namespace Llama.Extensions
{
    public static class SpanFloatExtensions
    {
        public static void Add(this Span<float> target, IEnumerable<KeyValuePair<int, float>> list)
        {
            foreach ((int key, float value) in list)
            {
                target[key] += value;
            }
        }

        public static void Update(this Span<float> target, IEnumerable<KeyValuePair<int, string>> list)
        {
            foreach ((int key, string value) in list)
            {
                float v;

                if (string.Equals("-inf", value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.NegativeInfinity;
                }
                else if (string.Equals("+inf", value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.PositiveInfinity;
                }
                else
                {
                    v = float.Parse(value);
                }

                target[key] = v;
            }
        }

        public static Dictionary<LlamaToken, float> Extract(this Span<float> source, IEnumerable<LlamaToken> list)
        {
            Dictionary<LlamaToken, float> toReturn = new();

            foreach (LlamaToken llamaToken in list)
            {
                toReturn.Add(llamaToken, source[llamaToken.Id]);
            }

            return toReturn;
        }

        public static void Update(this Span<float> target, IEnumerable<KeyValuePair<LlamaToken, float>> list)
        {
            foreach (KeyValuePair<LlamaToken, float> llamaToken in list)
            {
                target[llamaToken.Key.Id] = llamaToken.Value;
            }
        }
    }
}