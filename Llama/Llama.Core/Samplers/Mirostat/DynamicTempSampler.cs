using Llama.Core.Extensions;
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
        private readonly DynamicTempSamplerSettings _settings;

        private readonly Dictionary<char, float> _temperatureAdjustments = new()
        {
            [':'] = 6f,
            ['.'] = 4f,
            ['?'] = 4f,
            ['*'] = 6f
        };

        private float _target = 0.4f;

        public DynamicTempSampler(DynamicTempSamplerSettings settings)
        {
            _settings = settings;

            foreach (int id in this._settings.GreedyExclude)
            {
                _isWords.Add(id, true);
            }
        }

        public void ApplyOriginalMinP(SampleContext context)
        {
            Dictionary<int, int> mapping = new();

            Span<LlamaTokenData> newData = context.Candidates.Data.Span;

            for (int i = 0; i < context.Candidates.Data.Length; i++)
            {
                LlamaTokenData newToken = newData[i];
                mapping.Add(newToken.id, i);
            }

            foreach (LlamaTokenData token in context.OriginalCandidates)
            {
                float minp = this._settings.MinP;

                if (_settings.MinPs.TryGetValue(token.id, out float cminp))
                {
                    minp = Math.Max(minp, cminp);
                }

                if (token.p < minp)
                {
                    int newIndex = mapping[token.id];
                    context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                }
            }
        }

        public float CalculateNextTarget()
        {
            if (SelectionHistory == null || SelectionHistory.Count == 0)
            {
                throw new ArgumentException("Values list cannot be null or empty.");
            }

            // Calculate the sum of the values excluding the first element
            float sumExcludingFirst = SelectionHistory.Skip(1).Sum(l => l.p);

            // Calculate the next value needed to achieve the target average
            float nextValue = (this._settings.Target * QUEUE_SIZE) - sumExcludingFirst;

            return nextValue;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            //Softmax for backup
            SamplingApi.SoftMax(sampleContext.Candidates);
            this.ApplyOriginalMinP(sampleContext);
            SamplingApi.SoftMax(sampleContext.Candidates);

            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            float sampleTemp = _settings.Temperature;

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
                    LlamaTokenData token = candidateSpan[i];
                    totalDiff += Math.Abs(token.p - _target);
                }

                float scaledTotalDiff = totalDiff * (float)Math.Exp(1 - _settings.Scale);

                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    LlamaTokenData token = candidateSpan[i];
                    float diff = token.p - _target;
                    float absDiff = Math.Abs(diff);
                    float scaledDiff = absDiff * (float)Math.Exp(1 - _settings.Scale);
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
                    _target = this.CalculateNextTarget();
                    _target = Math.Clamp(_target, _settings.MinTarget, _settings.MaxTarget);
                }

                this.Push(this.GetOriginalData(sampleContext, selectedToken));
            }

            Debug.WriteLine($"({selectedToken}) T: {_target:0.00}; Avg: {average:0.00}; {candidateBuilder}");

            LlamaTokenData originalP = this.GetOriginalData(sampleContext, selectedToken);

            if (originalP.p < this._settings.MinP)
            {
                Debugger.Break();
            }

            return selectedToken;
        }
    }
}