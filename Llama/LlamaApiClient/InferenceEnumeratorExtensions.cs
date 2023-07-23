using LlamaApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Data.Extensions
{
    public static class InferenceEnumeratorExtensions
    {
        public static void SetLogits(this InferenceEnumerator enumerator, IEnumerable<KeyValuePair<int, string>> logits, LogitBiasLifeTime lifeTime)
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

                enumerator.SetLogit(iLogit.Key, v, lifeTime);
            }
        }
    }
}
