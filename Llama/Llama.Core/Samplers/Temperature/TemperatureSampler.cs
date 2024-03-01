using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Data.Native;
using Llama.Native;

namespace Llama.Core.Samplers.Temperature
{
    public class TemperatureSampler : ITokenSelector
    {
        protected readonly Dictionary<int, bool> _isWords = new();

        private readonly TemperatureSamplerSettings _settings;

        public TemperatureSampler(TemperatureSamplerSettings temperatureSamplerSettings)
        {
            _settings = temperatureSamplerSettings;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            return this.SelectToken(sampleContext, _settings.PreserveWords, out _);
        }

        protected bool CheckIfWord(SafeLlamaModelHandle ctx, int id)
        {
            if (!_isWords.TryGetValue(id, out bool word))
            {
                string value = NativeApi.TokenToPiece(ctx, id);
                word = string.IsNullOrWhiteSpace(value) || value[0] == ' ';
                _isWords[id] = word;
            }

            return word;
        }

        protected int SelectToken(SampleContext sampleContext, bool preserveWords, out bool topOnly)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);

            topOnly = false;
            int topToken = 0;

            if (preserveWords)
            {
                topToken = sampleContext.OriginalCandidates[0].id;
                topOnly = !this.CheckIfWord(sampleContext.ModelHandle, topToken);
            }

            int selectedToken;

            if (topOnly)
            {
                selectedToken = topToken;
            }
            else
            {
                SamplingApi.SoftMax(sampleContext.Candidates);
                SamplingApi.Temperature(sampleContext.Candidates, _settings.Temperature);
                selectedToken = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
            }

            return selectedToken;
        }
    }
}