using Llama.Core.Exceptions;
using Llama.Core.Extensions;
using Llama.Core.Interfaces;
using Llama.Core.Utils;
using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;
using Llama.Extensions;
using Llama.Native;

namespace Llama.Core
{
    public class LlamaContextWrapper : IContext
    {
        private readonly PointerArray<LlamaToken> _buffer;

        private readonly float[,] _embeddingStack;

        private readonly IExecutionScheduler _executionScheduler;

        private readonly KvCacheState<LlamaToken> _kvCache;

        private readonly LlamaContextSettings _settings;

        private readonly IList<ISimpleSampler> _simpleSamplers;

        private readonly ITokenSelector _tokenSelector;

        private readonly PointerArraySynchronizer<LlamaToken> _synchronizer;

        public LlamaContextWrapper(IExecutionScheduler executionScheduler, SafeLlamaContextHandle handle, SafeLlamaModelHandle modelHandle, LlamaContextSettings settings, IEnumerable<ISimpleSampler> simpleSamplers, ITokenSelector tokenSelector)
        {
            if (executionScheduler is null)
            {
                throw new ArgumentNullException(nameof(executionScheduler));
            }

            if (handle is null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (simpleSamplers is null)
            {
                throw new ArgumentNullException(nameof(simpleSamplers));
            }

            this._embeddingStack = new float[settings.ContextSize, 8192];

            for (int x = 0; x < settings.ContextSize; x++)
            {
                for (int y = 0; y < 8192; y++)
                {
                    this._embeddingStack[x, y] = float.NaN;
                }
            }

            _synchronizer = new PointerArraySynchronizer<LlamaToken>(
                new KvCacheShifter(settings.EvalThreadCount, settings.BatchSize, handle, modelHandle),
                new LlamaToken(-1, null)
                );

            this._executionScheduler = executionScheduler;
            this.Handle = handle;
            this._simpleSamplers = simpleSamplers.ToList();
            this._tokenSelector = tokenSelector ?? throw new ArgumentNullException(nameof(tokenSelector));
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.Size = this._settings.ContextSize;

            this._buffer = new PointerArray<LlamaToken>(this.Size);
            this._buffer.Fill(new LlamaToken(-1, null));

            this._kvCache = new KvCacheState<LlamaToken>(this.Size, new LlamaToken(-1, null));

            this.ModelHandle = modelHandle ?? throw new ArgumentNullException();

            if (!Directory.Exists("Logits"))
            {
                Directory.CreateDirectory("Logits");
            }
        }

        protected LlamaContextWrapper()
        {
        }

        public uint AvailableBuffer => this.Size - this._buffer.Pointer;

        public IReadOnlyLlamaTokenCollection Buffer => new LlamaTokenCollection(this._buffer);

        public IReadOnlyLlamaTokenCollection Evaluated => new LlamaTokenCollection(this._kvCache);

        public SafeLlamaContextHandle Handle { get; private set; }

        public SafeLlamaModelHandle ModelHandle { get; }

        public uint Size { get; private set; }

        public void Clear()
        {
            this._buffer.Clear();
        }

        public void Dispose() => this.Handle.Dispose();

        public void Evaluate(ExecutionPriority priority, int count = -1)
        {
            if (count != -1)
            {
                throw new NotImplementedException();
            }

            this.Ensure();

            _synchronizer.Sync(_kvCache, _buffer);
        }

        public LlamaToken SampleNext(LogitRuleCollection logitRules, ExecutionPriority priority) => this._executionScheduler.Execute(() => this.SampleTokenRaw(logitRules), priority);

        public LlamaToken SampleTokenRaw(LogitRuleCollection logitRules)
        {
            Span<float> logits = this.GetLogits();

            // Apply params.logit_bias map
            logits.Add(logitRules.OfType<LogitBias>());

            LlamaTokenDataArray candidates = new(logits);

            SamplingApi.SurpressNonEnglish(this.ModelHandle, candidates);
            SamplingApi.SoftMax(candidates);

            Dictionary<LlamaToken, float> no_penalize = logits.Extract(this.NoPenalize());

            SampleContext sampleContext = new()
            {
                Candidates = candidates,
                ContextHandle = Handle,
                ContextTokens = Evaluated,
                ModelHandle = ModelHandle,
                OriginalCandidates = new LlamaTokenData[candidates.Size]
            };

            SamplingApi.SoftMax(sampleContext.Candidates);
            Span<LlamaTokenData> target = new(sampleContext.OriginalCandidates, 0, sampleContext.OriginalCandidates.Length);
            sampleContext.Candidates.Data.Span.CopyTo(target);

            logitRules.StartClamp(sampleContext.Candidates);

            //TODO: Fix cheap hack
            foreach (ISimpleSampler simpleSampler in this._simpleSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            logitRules.ApplyPenalty(sampleContext.Candidates);

            sampleContext.Candidates.Update(no_penalize);

            logitRules.ApplyBias(sampleContext.Candidates);

            logitRules.ApplyClamp(sampleContext.Candidates);

            int tokenId = this._tokenSelector.SampleNext(sampleContext);

            LlamaToken toReturn = this.GetToken(tokenId);

            return toReturn;
        }

        public void SetBufferPointer(uint startIndex)
        {
            this._buffer.Pointer = startIndex;
        }

        public void Write(LlamaToken token)
        {
            if (this.AvailableBuffer == 0)
            {
                throw new OutOfContextException();
            }

            this._buffer[this._buffer.Pointer++] = token;
        }

        private LlamaTokenCollection NoPenalize()
        {
            LlamaTokenCollection collection = new();
            return collection;
        }
    }
}