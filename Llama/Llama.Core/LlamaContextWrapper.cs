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

            List<string> values = new(logits.Length);

            foreach (float logit in logits)
            {
                values.Add(logit.ToString());
            }

            // Apply params.logit_bias map
            logits.Add(logitRules.OfType<LogitBias>());

            LlamaTokenDataArray candidates = new(logits);

            //Move these somewhere else later.
            SamplingApi.SurpressNewline(this.ModelHandle, candidates);
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

            foreach (LogitClamp clamp in logitRules.OfType<LogitClamp>())
            {
                clamp.SetStart(sampleContext.GetProbability(clamp.LogitId));
            }

            //TODO: Fix cheap hack
            foreach (ISimpleSampler simpleSampler in this._simpleSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            //Apply penalty
            foreach (LogitPenalty penalty in logitRules.OfType<LogitPenalty>())
            {
                sampleContext.SetPenalty(penalty.LogitId, penalty.Value);
            }

            sampleContext.Update(no_penalize);

            //Apply bias
            foreach (LogitBias bias in logitRules.OfType<LogitBias>())
            {
                sampleContext.SetBias(bias.LogitId, bias.Value, bias.LogitBiasType);
            }

            //Apply clamping
            foreach (LogitClamp clamp in logitRules.OfType<LogitClamp>())
            {
                float nv = sampleContext.GetProbability(clamp.LogitId);
                float cv = clamp.GetValue(nv);

                if (cv != nv)
                {
                    sampleContext.SetProbability(clamp.LogitId, cv);
                }
            }

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
            //collection.Append(this.GetToken(13));//NL
            
            //collection.Append(this.GetToken(334));// *
            //collection.Append(this.GetToken(29930));//*
            
            //collection.Append(this.GetToken(368));//ly
            //collection.Append(this.GetToken(297));//in
            //collection.Append(this.GetToken(591));//we
            //collection.Append(this.GetToken(411));//with
            //collection.Append(this.GetToken(373));//on
            //collection.Append(this.GetToken(596));//your
            //collection.Append(this.GetToken(445));//this
            //collection.Append(this.GetToken(1048));//about
            //collection.Append(this.GetToken(408));//as
            //collection.Append(this.GetToken(367));//be
            //collection.Append(this.GetToken(338));//is
            //collection.Append(this.GetToken(470));//or
            //collection.Append(this.GetToken(727));//there
            //collection.Append(this.GetToken(267));//es
            ////collection.Append(this.GetToken(1749));//our
            //collection.Append(this.GetToken(541));//but
            //collection.Append(this.GetToken(769));//then
            //collection.Append(this.GetToken(515));//from
            //collection.Append(this.GetToken(451));//not
            //collection.Append(this.GetToken(491));//by
            //collection.Append(this.GetToken(577));//so
            //collection.Append(this.GetToken(502));//us
            //collection.Append(this.GetToken(526));//are
            //collection.Append(this.GetToken(437));//do
            //collection.Append(this.GetToken(565));//if
            //collection.Append(this.GetToken(471));//was
            //collection.Append(this.GetToken(2086));//too
            //collection.Append(this.GetToken(304));//to
            //collection.Append(this.GetToken(590));//my
            //collection.Append(this.GetToken(902));//her
            //collection.Append(this.GetToken(1075));//him
            //collection.Append(this.GetToken(670));//his
            //collection.Append(this.GetToken(7955));//hers
            ////collection.Append(this.GetToken(29892));//,
            ////collection.Append(this.GetToken(322));//and
            ////collection.Append(this.GetToken(306));//I
            //collection.Append(this.GetToken(310));//of
            //collection.Append(this.GetToken(29879));//s
            //collection.Append(this.GetToken(29991));//!
            //collection.Append(this.GetToken(278));//the
            //collection.Append(this.GetToken(592));//me
            //collection.Append(this.GetToken(263));//a
            //collection.Append(this.GetToken(363));//for
            //collection.Append(this.GetToken(372));//it
            //collection.Append(this.GetToken(393));//that
            return collection;
        }
    }
}