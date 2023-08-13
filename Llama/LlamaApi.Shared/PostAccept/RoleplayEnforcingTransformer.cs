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
            string? writtenTrimmed = enumerator.Enumerated.ToString()?.Trim();

            if (writtenTrimmed != null && !writtenTrimmed.Contains('*'))
            {
                float mod = writtenTrimmed.Length * this._slope;
                enumerator.SetBias(334, mod, LogitRuleLifetime.Token);
            }
        }
    }
}