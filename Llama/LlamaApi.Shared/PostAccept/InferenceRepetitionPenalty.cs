using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class InferenceRepetitionPenalty : IPostAccept
    {
        private readonly float _penalty;

        public InferenceRepetitionPenalty(float penalty)
        {
            this._penalty = penalty;
        }

        public void PostAccept(InferenceEnumerator enumerator)
        {
            foreach (LlamaToken token in enumerator.Enumerated)
            {
                enumerator.SetPenalty(token.Id, _penalty, LogitRuleLifetime.Inferrence);
            }
        }
    }
}