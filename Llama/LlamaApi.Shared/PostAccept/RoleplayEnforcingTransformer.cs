using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class RoleplayEnforcingTransformer : IPostAccept
    {
        private readonly float _slope;

        public RoleplayEnforcingTransformer(float slope)
        {
            this._slope = slope;
        }

        public void PostAccept(InferenceEnumerator enumerator)
        {
            string writtenTrimmed = enumerator.Enumerated.ToString()?.Trim() ?? string.Empty;

            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            bool endsWith = writtenTrimmed.EndsWith("*");

            if (asteriskCount == 0)
            {
                float mod = writtenTrimmed.Length * this._slope;
                enumerator.SetBias(334, mod, LogitRuleLifetime.Token);
            }

            if((asteriskCount >= 4 && asteriskCount % 2 == 0) || endsWith) 
            {
                enumerator.SetBias(334, float.NegativeInfinity, LogitRuleLifetime.Token);
            }
        }
    }
}