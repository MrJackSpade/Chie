using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
    public class DynamicTempSampler : BaseDynamicSampler, ITokenSelector
    {
        private readonly float _initialTarget = 0.4f;

        private readonly float MIN_P = .01f;

        private readonly DynamicTempSamplerSettings _settings;

        private float _target = 0.4f;

        private readonly Dictionary<char, float> _temperatureAdjustments = new()
        {
            [':'] = 3f,
            ['.'] = 2f,
            ['?'] = 2f,
            ['*'] = 3f
        };

        public DynamicTempSampler(DynamicTempSamplerSettings settings)
        {
            _settings = settings;
        }

        public float CalculateNextTarget()
        {
            if (this.SelectionHistory == null || this.SelectionHistory.Count == 0)
            {
                throw new ArgumentException("Values list cannot be null or empty.");
            }

            // Calculate the sum of the values excluding the first element
            float sumExcludingFirst = this.SelectionHistory.Skip(1).Sum(l => l.p);

            // Calculate the next value needed to achieve the target average
            float nextValue = (this._initialTarget * QUEUE_SIZE) - sumExcludingFirst;

            return nextValue;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            //Softmax for backup
            SamplingApi.SoftMax(sampleContext.Candidates);          
            SamplingApi.MinP(sampleContext.Candidates, MIN_P);
            SamplingApi.SoftMax(sampleContext.Candidates);

            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            float sampleTemp = this._settings.Temperature;

            if (sampleContext.ContextTokens.Count > 0)
            {
                LlamaToken token = sampleContext.ContextTokens.Trim().Last();

                string value = NativeApi.TokenToPiece(sampleContext.ModelHandle, token.Id);

                if (!string.IsNullOrEmpty(value) && _temperatureAdjustments.TryGetValue(value[^1], out float f))
                {
                    sampleTemp = f;
                }
            }

            if (this.TryGetQueueAverage(out float average))
            {
                float totalDiff = 0;

                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    var token = candidateSpan[i];
                    totalDiff += Math.Abs(token.p - _target);
                }

                float scaledTotalDiff = totalDiff * (float)Math.Exp(1 - this._settings.Scale);

                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    var token = candidateSpan[i];
                    float diff = token.p - _target;
                    float absDiff = Math.Abs(diff);
                    float scaledDiff = absDiff * (float)Math.Exp(1 - this._settings.Scale);
                    float perDiff = scaledDiff / scaledTotalDiff;
                    float perInvDiff = 1 - perDiff;
                    float adjTemp = sampleTemp / perInvDiff;
                    SamplingApi.Temperature(sampleContext.Candidates, i, adjTemp);
                }
            }
            else
            {
                SamplingApi.Temperature(sampleContext.Candidates, sampleTemp);
            }

            SamplingApi.TailFree(sampleContext.Candidates, _settings.Tfs, 1);

            int selectedToken = this.SelectToken(sampleContext, _settings.PreserveWords, out bool topOnly);

            // Compute error as the difference between observed surprise and target surprise value

            StringBuilder candidateBuilder = new();

            this.WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            if ((!topOnly || _settings.FactorPreservedWords))
            {
                if (this.TryGetQueueAverage(out average))
                {
                    this._target = this.CalculateNextTarget();
                    this._target = Math.Clamp(this._target, this._settings.MinTarget, this._settings.MaxTarget);
                }

                this.Push(this.GetOriginalData(sampleContext, selectedToken));
            }

            Debug.WriteLine($"({selectedToken}) T: {_target:0.00}; Avg: {average:0.00}; {candidateBuilder}");

            return selectedToken;
        }
    }
}