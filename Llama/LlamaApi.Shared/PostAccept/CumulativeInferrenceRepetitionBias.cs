using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class CumulativeInferrenceRepetitionBias : IPostAccept
    {
        private readonly float _minCount;

        private readonly float _penalty;

        public CumulativeInferrenceRepetitionBias(float penalty, int minCount)
        {
            this._penalty = penalty;
            this._minCount = minCount;
        }

        public async Task PostAccept(InferenceEnumerator enumerator)
        {
            Dictionary<int, float> tokens = new();

            foreach (LlamaToken token in enumerator.Enumerated)
            {
                if (!tokens.ContainsKey(token.Id))
                {
                    tokens.Add(token.Id, 1);
                }
                else
                {
                    tokens[token.Id] += 1;
                }
            }

            foreach (KeyValuePair<int, float> token in tokens)
            {
                if (token.Value >= this._minCount)
                {
                    enumerator.SetBias(token.Key, 0 - (float)Math.Pow(_penalty, token.Value), LogitRuleLifetime.Token, LogitBiasType.Additive);
                }
            }
        }
    }
}