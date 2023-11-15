﻿using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatTempSampler : ITokenSelector
    {
        private readonly Dictionary<int, bool> _isWords = new();

        private readonly MirostatTempSamplerSettings _settings;

        private float _mu;

        private float _temp;

        private LlamaTokenData[] _tempCandidates;

        public MirostatTempSampler(MirostatTempSamplerSettings settings)
        {
            this._settings = settings;
            this._mu = settings.InitialMu;
            this._temp = settings.InitialTemperature;
        }

        public static int Clamp(float k)
        {
            if (k <= 0)
            {
                return 0;
            }
            else if (k >= int.MaxValue)
            {
                return int.MaxValue;
            }
            else
            {
                return (int)k;
            }
        }

        public string GetDisplayString(SampleContext ctx, LlamaTokenData data)
        {
            LlamaToken token = this.GetToken(ctx, data.id);

            return $"{token.GetEscapedValue()} ({data.p:0.00})";
        }

        public LlamaToken GetToken(SampleContext ctx, int id) => new(id, NativeApi.TokenToPiece(ctx.ModelHandle, id));

        public int SampleNext(SampleContext sampleContext)
        {
            //Softmax for backup
            SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);
            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;
            this.Copy(candidateSpan);

            SamplingApi.TailFree(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tfs, 1);
            SamplingApi.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._temp);
            SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);

            float tau = this._settings.Target;
            float eta = this._settings.LearningRate;

            bool topOnly = false;
            int top_x = 0;

            if (this._settings.PreserveWords)
            {
                top_x = SamplingApi.TokenGreedy(sampleContext.ContextHandle, sampleContext.Candidates);
                topOnly = !this.CheckIfWord(sampleContext.ModelHandle, top_x);
            }

            int x;

            if (topOnly)
            {
                x = top_x;
            }
            else
            {
                SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);
                x = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
            }

            // Compute error as the difference between observed surprise and target surprise value
            int x_idx = 0;

            for (int i = 0; i < (int)sampleContext.Candidates.Size; i++)
            {
                if (candidateSpan[i].id == x)
                {
                    x_idx = i;
                    break;
                }
            }

            StringBuilder candidateBuilder = new();

            float selectedP = candidateSpan[x_idx].p;

            candidateBuilder.Append($"[{this.GetDisplayString(sampleContext, candidateSpan[x_idx])}] || ");

            ulong displayCount = Math.Min(10, sampleContext.Candidates.Size);

            if(topOnly)
            {
                displayCount = 1;
            }

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

                candidateBuilder.Append(this.GetDisplayString(sampleContext, candidateSpan[i]));
            }

            candidateBuilder.Append(']');

            //Calculate surprise based on the original P to
            //ensure that wonky probability fuckery doesn't mess
            //up the surprise calculations
            float original_p = this.GetOriginalP(x);
            
            string muCalc = string.Empty;

            if (!topOnly || this._settings.FactorPreservedWords)
            {
                float observed_surprise = -(float)(Math.Log(original_p) / Math.Log(2));
                float e = observed_surprise - tau;

                // Update mu using the learning rate and error
                float adj = eta * e;
                float nuMu = this._mu - adj;
                float nuTemp = this._temp - adj * this._settings.TemperatureLearningRate;

                if (nuTemp > 0 && !float.IsNaN(nuTemp) && !float.IsInfinity(nuTemp) && !float.IsNaN(nuMu) && !float.IsInfinity(nuMu))
                {
                    this._temp = nuTemp;
                    this._mu = nuMu;
                }

                muCalc = $"Ob: {observed_surprise:0.00}; Adj: {adj:0.00};";
            }

            Debug.WriteLine($"T: {this._temp:0.00}; Mu: {this._mu:0.00}; {muCalc} {candidateBuilder}");

            return x;
        }

        private bool CheckIfWord(SafeLlamaModelHandle ctx, int id)
        {
            if (!this._isWords.TryGetValue(id, out bool word))
            {
                string value = NativeApi.TokenToPiece(ctx, id);
                word = !string.IsNullOrWhiteSpace(value) && !char.IsLetter(value[0]);
                this._isWords.Add(id, word);
            }

            return word;
        }

        private void Copy(Span<LlamaTokenData> sampleContext)
        {
            this._tempCandidates ??= new LlamaTokenData[sampleContext.Length];
            Span<LlamaTokenData> target = new(this._tempCandidates, 0, this._tempCandidates.Length);
            sampleContext.CopyTo(target);
        }

        private float GetOriginalP(int id)
        {
            foreach (LlamaTokenData ltd in this._tempCandidates)
            {
                if (ltd.id == id)
                {
                    return ltd.p;
                }
            }

            throw new InvalidDataException();
        }
    }
}