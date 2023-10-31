using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;

namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatOneSampler : ITokenSelector
    {
        private readonly Dictionary<int, bool> _isWords = new();

        private readonly MirostatSamplerSettings _settings;

        private float _mu;

        public MirostatOneSampler(MirostatSamplerSettings settings)
        {
            this._settings = settings;
            this._mu = settings.InitialMu;
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

        public int SampleNext(SampleContext sampleContext)
        {
            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.data.Span;

            SamplingApi.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Temperature);

            float tau = this._settings.Tau;
            float eta = this._settings.Eta;
            float m = this._settings.M;

            float n = sampleContext.Candidates.data.Length;

            SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);

            float sum_ti_bi = 0.0f;
            float sum_ti_sq = 0.0f;

            for (int i = 0; i < m - 1 && i < (int)sampleContext.Candidates.size - 1; i++)
            {
                float ti = (float)Math.Log((i + 2) / (double)(i + 1));
                float b_i = (float)Math.Log(candidateSpan[i].p / candidateSpan[i + 1].p);
                sum_ti_bi += ti * b_i;
                sum_ti_sq += ti * ti;
            }

            // Estimate s_hat using the most probable m tokens
            float hat = sum_ti_bi / sum_ti_sq;

            // Compute k from the estimated s_hat and target surprise value
            float epsilon_hat = hat - 1;

            float k = (float)Math.Pow((epsilon_hat * Math.Pow(2, _mu)) / (1 - Math.Pow(n, -epsilon_hat)), 1 / hat);

            Debug.WriteLine($"k: {k}");

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
                int ki = Clamp(k);
                // Sample the next word X using top-k sampling
                SamplingApi.TopK(sampleContext.ContextHandle, sampleContext.Candidates, ki, 1);
                x = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
            }

            // Compute error as the difference between observed surprise and target surprise value
            int x_idx = 0;

            for (int i = 0; i < (int)sampleContext.Candidates.size; i++)
            {
                if (sampleContext.Candidates.data.Span[i].id == x)
                {
                    x_idx = i;
                    break;
                }
            }

            float observed_surprise = -(float)(Math.Log(sampleContext.Candidates.data.Span[x_idx].p) / Math.Log(2));
            float e = observed_surprise - tau;

            // Update mu using the learning rate and error
            _mu -= eta * e;

            Debug.WriteLine($"mu: {_mu}");

            return x;
        }

        private bool CheckIfWord(SafeLlamaModelHandle ctx, int id)
        {
            if (!_isWords.TryGetValue(id, out bool word))
            {
                string value = NativeApi.TokenToPiece(ctx, id);
                word = !string.IsNullOrWhiteSpace(value) && value[0] == ' ';
                _isWords.Add(id, word);
            }

            return word;
        }
    }
}