using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class TextExtensionTransformer : ITokenTransformer
    {
        private readonly int _max;

        private readonly int _min;

        private readonly Random _random = new();

        public TextExtensionTransformer(int min, int max)
        {
            this._min = min;
            this._max = max;
        }

        public async IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            string written = enumerator.Enumerated.ToString() ?? string.Empty;

            float chance = 1 - (written.Length - _min) / (float)(this._max - _min);

            bool extend = this._random.NextDouble() < chance;

            await foreach (LlamaToken lt in selectedTokens)
            {
                if (written.Length > this._max || lt.Id != LlamaToken.EOS.Id || !extend)
                {
                    yield return lt;
                    continue;
                }
                else
                {
                    enumerator.SetBias(LlamaToken.EOS.Id, float.NegativeInfinity, LogitRuleLifetime.Token);
                    enumerator.SetBias(LlamaToken.NewLine.Id, float.NegativeInfinity, LogitRuleLifetime.Token);
                }
            }
        }
    }
}