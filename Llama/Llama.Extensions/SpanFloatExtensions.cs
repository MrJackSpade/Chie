using Llama.Data.Models;
using System.Diagnostics;

namespace Llama.Extensions
{
    public static class SpanFloatExtensions
    {
        public static void Add(this Span<float> target, IEnumerable<LogitBias> list)
        {
            foreach (LogitBias bias in list)
            {
                target[bias.LogitId] += bias.Value;
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

        public static void Update(this Span<float> target, IEnumerable<KeyValuePair<LlamaToken, float>> list)
        {
            foreach (KeyValuePair<LlamaToken, float> llamaToken in list)
            {
                if (target[llamaToken.Key.Id] != llamaToken.Value)
                {
                    Debug.Write($"Adjusting logit [{llamaToken.Key.Id}]; '{target[llamaToken.Key.Id]}' => '{llamaToken.Value}'");
                    target[llamaToken.Key.Id] = llamaToken.Value;
                }
            }
        }
    }
}