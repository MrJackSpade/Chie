using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
    public abstract class BaseDynamicSampler
    {
        protected readonly Dictionary<int, bool> _isWords = new();

        protected readonly Queue<LlamaTokenData> SelectionHistory = new();

        protected readonly int QUEUE_SIZE = 3;

        public string GetDisplayString(SampleContext ctx, int tokenId)
        {
            LlamaTokenData tokenData = new();

            for (int i = 0; i < ctx.OriginalCandidates.Length; i++)
            {
                if (ctx.OriginalCandidates[i].id == tokenId)
                {
                    tokenData = ctx.OriginalCandidates[i];
                    break;
                }
            }

            LlamaTokenData newTokenData = new();

            for (int i = 0; i < ctx.Candidates.Data.Length; i++)
            {
                if (ctx.Candidates.Data.Span[i].id == tokenId)
                {
                    newTokenData = ctx.Candidates.Data.Span[i];
                    break;
                }
            }

            LlamaToken token = this.GetToken(ctx, tokenData.id);

            return $"{token.GetEscapedValue()} ({tokenData.p:0.00} => {newTokenData.p:0.00})";
        }

        public LlamaToken GetToken(SampleContext ctx, int id)
        {
            return new(id, NativeApi.TokenToPiece(ctx.ModelHandle, id));
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

        protected LlamaTokenData GetOriginalData(SampleContext sampleContext, int tokenId)
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

        protected void Push(LlamaTokenData token)
        {
            SelectionHistory.Enqueue(token);

            if (SelectionHistory.Count > QUEUE_SIZE)
            {
                SelectionHistory.Dequeue();
            }
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
                selectedToken = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
            }

            return selectedToken;
        }

        protected bool TryGetQueueAverage(out float avg)
        {
            avg = 0f;
            if (SelectionHistory.Count < QUEUE_SIZE)
            {
                return false;
            }
            else
            {
                avg = SelectionHistory.Average(l => l.p);
                return true;
            }
        }

        protected void WriteToLog(SampleContext sampleContext, Span<LlamaTokenData> candidateSpan, bool topOnly, int selectedToken, StringBuilder candidateBuilder)
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
    }
}