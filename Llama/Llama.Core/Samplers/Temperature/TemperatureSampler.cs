using Llama.Core.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Text;

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
            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            topOnly = false;
            int topToken = 0;

            if (preserveWords)
            {
                topToken = sampleContext.OriginalCandidates[0].id;
                topOnly = !_settings.GreedyInclude.Contains(topToken) &&
                                (!this.CheckIfWord(sampleContext.ModelHandle, topToken) ||
                                _settings.GreedyInclude.Contains(topToken));
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

            StringBuilder logBuilder = new();
            this.WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, logBuilder);
            
            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] ({selectedToken}) {logBuilder}");

            return selectedToken;
        }

        protected void WriteToLog(SampleContext sampleContext, Span<LlamaTokenData> candidateSpan, bool topOnly, int selectedToken, StringBuilder candidateBuilder)
        {
            if (topOnly)
            {
                candidateBuilder.Append(" [SINGLE] [");
                candidateBuilder.Append(sampleContext.GetDisplayString(selectedToken));
            }
            else
            {
                candidateBuilder.Append($"[{sampleContext.GetDisplayString(selectedToken)}] || ");

                ulong displayCount = Math.Min(10, sampleContext.Candidates.Size);

                for (int i = 0; i < (int)displayCount; i++)
                {
                    if (candidateSpan[i].p == 0)
                    {
                        break;
                    }

                    if (i > 0)
                    {
                        candidateBuilder.Append(" | ");
                    }

                    candidateBuilder.Append(sampleContext.GetDisplayString(candidateSpan[i].id));
                }
            }

            candidateBuilder.Append(']');
        }
    }
}