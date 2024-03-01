using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatTempSampler : BaseDynamicSampler, ITokenSelector
    {
        readonly Dictionary<char, float> _temperatureAdjustments = new()
        {
            [':'] = 3f,
            ['.'] = 2f,
            ['?'] = 2f,
            ['*'] = 3f
        };

        private readonly Dictionary<int, bool> _isWords = new();

        private readonly MirostatTempSamplerSettings _settings;

        private float _temp;

        private readonly float LEARNING_SLOPE = 0.8f;

        private readonly float MIN_TEMP = .4f;

        private readonly float TARGET = 0.4f;

        public MirostatTempSampler(MirostatTempSamplerSettings settings) : base(3)
        {
            _settings = settings;
            _temp = settings.InitialTemperature;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            //Softmax for backup
            SamplingApi.SoftMax(sampleContext.Candidates);
            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            float sampleTemp = _temp;

            if (sampleContext.ContextTokens.Count > 0)
            {
                LlamaToken token = sampleContext.ContextTokens.Trim().Last();

                string value = NativeApi.TokenToPiece(sampleContext.ModelHandle, token.Id);

                if (!string.IsNullOrEmpty(value) && _temperatureAdjustments.TryGetValue(value[^1], out float f))
                {
                    sampleTemp = f;
                }
            }

            SamplingApi.Temperature(sampleContext.Candidates, sampleTemp);
            SamplingApi.TailFree(sampleContext.Candidates, _settings.Tfs, 1);
            SamplingApi.SoftMax(sampleContext.Candidates);

            int selectedToken = this.SelectToken(sampleContext, _settings.PreserveWords, out bool topOnly);

            // Compute error as the difference between observed surprise and target surprise value

            StringBuilder candidateBuilder = new();

            this.WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            float average = 0f;

            if ((!topOnly || _settings.FactorPreservedWords))
            {
                if (this.TryGetQueueAverage(out average))
                {
                    float dif = TARGET - average;

                    float adj = dif * LEARNING_SLOPE;

                    _temp -= adj;

                    _temp = Math.Max(_temp, MIN_TEMP);

                    _temp = Math.Min(_temp, this._settings.MaxTemp);
                }

                this.Push(this.GetOriginalData(sampleContext, selectedToken));
            }

            Debug.WriteLine($"T: {_temp:0.00}; Mu: {average:0.00}; {candidateBuilder}");

            return selectedToken;
        }
    }
}