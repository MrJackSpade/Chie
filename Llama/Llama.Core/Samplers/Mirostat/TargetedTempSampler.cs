using Llama.Core.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
    public class TargetedTempSampler : BaseDynamicSampler, ITokenSelector
    {
        private readonly TargetedTempSamplerSettings _settings;

        private float _target = 1f;

        public TargetedTempSampler(TargetedTempSamplerSettings settings) : base(settings.QueueSize, settings)
        {
            _settings = settings;

            foreach (int id in this._settings.GreedyExclude)
            {
                _isWords.Add(id, true);
            }
        }

        public void ApplyOriginalMinP(SampleContext context)
        {
			SamplingApi.SoftMax(context.Candidates);

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

                    //Don't apply it to the most likely new token.
                    if (newIndex > 0)
                    {
                        context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                    }
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
            float nextValue = (this._settings.Target * QueueSize) - sumExcludingFirst;

            return nextValue;
        }

        public int SampleNext(SampleContext sampleContext)
        {
            int ts = 0;

            for (int i = 0; i < sampleContext.Candidates.Data.Length; i++)
            {
                LlamaTokenData newToken = sampleContext.Candidates.Data.Span[i];

                if (newToken.p > 0.001f)
                {
                    ts++;
                }
            }

            //Softmax for backup
            this.ApplyOriginalMinP(sampleContext);
            SamplingApi.SoftMax(sampleContext.Candidates);

            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            float sampleTemp = _settings.Temperature;

            if (sampleContext.ContextTokens.Count > 0)
            {
                LlamaToken token = sampleContext.ContextTokens.Trim().Last();

                string value = NativeApi.TokenToPiece(sampleContext.ModelHandle, token.Id);
            }

            if (this.TryGetQueueAverage(out float average))
            {
                float totalDiff = 0;

                float c_target = _target;
                float c_min = float.MaxValue;
                float c_max = float.MinValue;

                //Find the real target range. This is important because if we're
                //lower than the lowest token, we actually scale back to normal distribution
                //likewise with max
                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    LlamaTokenData token = candidateSpan[i];

                    if (token.p > this._settings.MinP)
                    {
                        c_min = Math.Min(token.p, c_min);
                    }

                    c_max = Math.Max(token.p, c_max);
                }

                //clamp the target to the real range
                c_target = Math.Max(c_target, c_min);
                c_target = Math.Min(c_target, c_max);

                //Find the difference total difference between the real target and
                //each token
                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    LlamaTokenData token = candidateSpan[i];
                    totalDiff += Math.Abs(token.p - c_target);
                }

                //Now calculate the proportion of the distance and apply scaling
                float scaledTotalDiff = totalDiff * (float)Math.Exp(1 - _settings.Scale);
                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    LlamaTokenData token = candidateSpan[i];
                    float diff = token.p - c_target;
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

            int selectedToken = this.SelectToken(sampleContext, _settings.PreserveWordMinP, _settings.PreserveWordMaxP, out bool topOnly);

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

                this.Push(sampleContext.GetOriginalData(selectedToken));
            }

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [{ts}] ({selectedToken}) T: {_target:0.00}; Avg: {average:0.00}; {candidateBuilder}");

            LlamaTokenData originalP = sampleContext.GetOriginalData(selectedToken);
            LlamaTokenData current = sampleContext.GetData(selectedToken);

            if (originalP.p < this._settings.MinP)
            {
                Debug.WriteLine("Token below min-p selected");
            }

            return selectedToken;
        }
    }
}