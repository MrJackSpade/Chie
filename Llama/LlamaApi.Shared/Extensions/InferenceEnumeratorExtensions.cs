using Llama.Data.Models;
using LlamaApiClient;

namespace Llama.Data.Extensions
{
    public static class InferenceEnumeratorExtensions
    {
        public static void SetBias(this InferenceEnumerator enumerator, IEnumerable<KeyValuePair<int, string>> logits, LogitRuleLifetime lifeTime, LogitBiasType logitBiasType)
        {
            foreach (KeyValuePair<int, string> iLogit in logits)
            {
                float v;

                if (string.Equals("-inf", iLogit.Value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.NegativeInfinity;
                }
                else if (string.Equals("+inf", iLogit.Value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.PositiveInfinity;
                }
                else
                {
                    v = float.Parse(iLogit.Value);
                }

                enumerator.AddLogitRule(new LogitBias(iLogit.Key, v, lifeTime, logitBiasType));
            }
        }

        public static void SetBias(this InferenceEnumerator enumerator, int id, float value, LogitRuleLifetime lifeTime, LogitBiasType logitBiasType) => enumerator.AddLogitRule(new LogitBias(id, value, lifeTime, logitBiasType));

        public static void SetPenalty(this InferenceEnumerator enumerator, IEnumerable<KeyValuePair<int, string>> logits, LogitRuleLifetime lifeTime)
        {
            foreach (KeyValuePair<int, string> iLogit in logits)
            {
                float v;

                if (string.Equals("-inf", iLogit.Value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.NegativeInfinity;
                }
                else if (string.Equals("+inf", iLogit.Value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.PositiveInfinity;
                }
                else
                {
                    v = float.Parse(iLogit.Value);
                }

                enumerator.AddLogitRule(new LogitPenalty(iLogit.Key, v, lifeTime));
            }
        }

        public static void SetPenalty(this InferenceEnumerator enumerator, int id, float value, LogitRuleLifetime lifeTime) => enumerator.AddLogitRule(new LogitPenalty(id, value, lifeTime));
    }
}