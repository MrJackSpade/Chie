using Llama.Core.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatTempSampler : ITokenSelector
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

        private float LEARNING_SLOPE = 0.8f;

        private float MIN_TEMP = .4f;

        private float MAX_TEMP = 1.25f;

        private float TARGET = 0.4f;

        private int QUEUE_SIZE = 3;

        private Queue<LlamaTokenData> _selectionHistory = new();

        public MirostatTempSampler(MirostatTempSamplerSettings settings)
        {
            this._settings = settings;
            this._temp = settings.InitialTemperature;
        }

        public string GetDisplayString(SampleContext ctx, int tokenId)
        {
            LlamaTokenData tokenData = new();

            for(int i = 0; i < ctx.OriginalCandidates.Length; i++)
            {
                if (ctx.OriginalCandidates[i].id == tokenId)
                {
                    tokenData = ctx.OriginalCandidates[i];
                    break;
                }
            }

            LlamaToken token = this.GetToken(ctx, tokenData.id);

            return $"{token.GetEscapedValue()} ({tokenData.p:0.00})";
        }

        public LlamaToken GetToken(SampleContext ctx, int id) => new(id, NativeApi.TokenToPiece(ctx.ModelHandle, id));

        private void Push(LlamaTokenData token)
        {
            _selectionHistory.Enqueue(token);

            if(_selectionHistory.Count > QUEUE_SIZE)
            {
                _selectionHistory.Dequeue();
            }
        }

        private bool TryGetQueueAverage(out float avg)
        {
            avg = 0f;
            if(_selectionHistory.Count < QUEUE_SIZE)
            {
                return false;
            } else
            {
                avg = _selectionHistory.Average(l => l.p);
                return true;
            }
        }

        public int SampleNext(SampleContext sampleContext)
        {
            //Softmax for backup
            SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);
            Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            float sampleTemp = this._temp;

            if(sampleContext.ContextTokens.Count > 0)
            {
                LlamaToken token = sampleContext.ContextTokens.Trim().Last();

                string value = NativeApi.TokenToPiece(sampleContext.ModelHandle, token.Id);

                if(!string.IsNullOrEmpty(value) && _temperatureAdjustments.TryGetValue(value[^1], out float f))
                {
                    sampleTemp = f;
                }
            }

            SamplingApi.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, sampleTemp);
            SamplingApi.TailFree(sampleContext.ContextHandle, sampleContext.Candidates, this._settings.Tfs, 1);
            SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);

            bool topOnly = false;
            int topToken = 0;

            if (this._settings.PreserveWords)
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
                SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);
                selectedToken = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
            }

            // Compute error as the difference between observed surprise and target surprise value

            StringBuilder candidateBuilder = new();

            WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            float average = 0f;

            if ((!topOnly || this._settings.FactorPreservedWords))
            {
                if (TryGetQueueAverage(out average))
                {
                    float dif = TARGET - average;

                    float adj = dif * LEARNING_SLOPE;

                    this._temp -= adj;

                    this._temp = Math.Max(this._temp, MIN_TEMP);

                    this._temp = Math.Min(this._temp, MAX_TEMP);
                }

                Push(GetOriginalData(sampleContext, selectedToken));
            }

            Debug.WriteLine($"T: {this._temp:0.00}; Mu: {average:0.00}; {candidateBuilder}");

            return selectedToken;
        }

        private LlamaTokenData GetOriginalData(SampleContext sampleContext, int tokenId)
        {
            LlamaTokenData[] candidates = sampleContext.OriginalCandidates;
            
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].id == tokenId)
                {
                    return candidates[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

        private void WriteToLog(SampleContext sampleContext, Span<LlamaTokenData> candidateSpan, bool topOnly, int selectedToken, StringBuilder candidateBuilder)
        {
            if (topOnly)
            {
                candidateBuilder.Append(" [SINGLE] [");
                candidateBuilder.Append(this.GetDisplayString(sampleContext, selectedToken));
            }
            else
            {
                candidateBuilder.Append($"[{this.GetDisplayString(sampleContext, selectedToken)}] || ");

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

                    candidateBuilder.Append(this.GetDisplayString(sampleContext, candidateSpan[i].id));
                }
            }

            candidateBuilder.Append(']');
        }

        private bool CheckIfWord(SafeLlamaModelHandle ctx, int id)
        {
            if (!this._isWords.TryGetValue(id, out bool word))
            {
                string value = NativeApi.TokenToPiece(ctx, id);
                word = !string.IsNullOrWhiteSpace(value) && !(char.IsLetter(value[0]) || value[0] == '\'');
                this._isWords.Add(id, word);
            }

            return word;
        }
    }
}