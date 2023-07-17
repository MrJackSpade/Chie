using Llama.Core.Exceptions;
using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Enums;
using Llama.Data.Exceptions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;
using Llama.Extensions;
using Llama.Native;
using System.Diagnostics;

namespace Llama.Core
{
    public class LlamaContextWrapper : IContext
    {
        private readonly LlamaTokenCollection _buffer;

        private readonly int _evalThreadCount;

        private readonly LlamaTokenCollection _evaluated;

        private readonly IExecutionScheduler _executionScheduler;

        private readonly LlamaContextSettings _settings;

        private readonly IList<ISimpleSampler> _simpleSamplers;

        private readonly ITokenSelector _tokenSelector;

        private int _bufferPointer = 0;

        private int _evalPointer = 0;

        public LlamaContextWrapper(IExecutionScheduler executionScheduler, SafeLlamaContextHandle handle, LlamaContextSettings settings, IEnumerable<ISimpleSampler> simpleSamplers, ITokenSelector tokenSelector)
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

            this._evalThreadCount = settings.EvalThreadCount;
            this._executionScheduler = executionScheduler;
            this.Handle = handle;
            this._simpleSamplers = simpleSamplers.ToList();
            this._tokenSelector = tokenSelector ?? throw new ArgumentNullException(nameof(tokenSelector));
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.Size = this._settings.ContextSize;
            this._buffer = new LlamaTokenBuffer(this.Size);
            this._evaluated = new LlamaTokenBuffer(this.Size);
        }

        protected LlamaContextWrapper()
        {
        }

        public int AvailableBuffer => this.Size - this._bufferPointer;

        public IReadOnlyLlamaTokenCollection Buffer => this._buffer;

        public IReadOnlyLlamaTokenCollection Evaluated => this._evaluated;

        public SafeLlamaContextHandle Handle { get; private set; }

        public int Size { get; private set; }

        public void Clear()
        {
            this._buffer.Clear();
            this._bufferPointer = 0;
            this._evalPointer = 0;
        }

        public void Dispose() => this.Handle.Dispose();

        public int Evaluate(ExecutionPriority priority, int count = -1)
        {
            if (count != -1)
            {
                throw new NotImplementedException();
            }

            this.Ensure();

            int start = this._evalPointer;

            int end = this._bufferPointer;

            LlamaTokenCollection toEvaluate = new(this._buffer.Skip(start).Take(end - start));

            // evaluate tokens in batches
            // embed is typically prepared beforehand to fit within a batch, but not always
            for (int i = 0; i < toEvaluate.Count; i += this._settings.BatchSize)
            {
                int n_eval = toEvaluate.Count - i;

                if (n_eval > this._settings.BatchSize)
                {
                    n_eval = this._settings.BatchSize;
                }

                LlamaTokenCollection thisBlock = new(toEvaluate.Skip(i).Take(n_eval));

                int[] tokens = thisBlock.Ids.ToArray();

                try
                {
                    Debug.WriteLine($"{this._evalPointer + 1}/{end}");

                    this._executionScheduler.Execute(() => this.NativeEval(tokens), priority);
                }
                catch (Exception e) when (Debugger.IsAttached)
                {
                    Debug.WriteLine(e);
                    Debugger.Break();
                }

                for (int c = 0; c < n_eval; c++)
                {
                    this._evaluated[c + this._evalPointer] = this._buffer[c + this._evalPointer];
                }

                this._evalPointer += n_eval;
            }

            for (int i = this._evalPointer; i < this.Evaluated.Count; i++)
            {
                this._evaluated[i] = LlamaToken.Null;
            }

            //The entirety of the token data needs to be synced for all tokens regardless
            //once the eval is complete, because otherwise metadata wont be copied across
            //The copy call above only intends on copying for the sake of the modification
            //event but an additional "full sync" call is needed.
            for (int i = 0; i < this._evaluated.Count; i++)
            {
                this._evaluated[i] = this._buffer[i];
            }

            return toEvaluate.Count;
        }

        public LlamaToken SampleNext(Dictionary<int, float> logitBias, ExecutionPriority priority) => this._executionScheduler.Execute(() => this.SampleTokenRaw(logitBias), priority);

        public LlamaToken SampleTokenRaw(Dictionary<int, float> logitBias)
        {
            Span<float> logits = this.GetLogits();

            // Apply params.logit_bias map
            logits.Add(this._settings.LogitBias);

            LlamaTokenDataArray candidates = new(logits);

            Dictionary<LlamaToken, float> no_penalize = logits.Extract(this.NoPenalize());

            SampleContext sampleContext = new()
            {
                Candidates = candidates,
                ContextHandle = Handle,
                ContextTokens = Evaluated
            };

            foreach (ISimpleSampler simpleSampler in this._simpleSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            logits.Update(no_penalize);

            foreach (KeyValuePair<int, float> bias in logitBias)
            {
                sampleContext.SetProbability(bias.Key, bias.Value);
            }

            int tokenId = this._tokenSelector.SampleNext(sampleContext);

            return this.GetToken(tokenId, LlamaTokenType.Response);
        }

        public void SetBufferPointer(int startIndex)
        {
            this._bufferPointer = startIndex;

            if (this._evalPointer >= this._bufferPointer)
            {
                this._evalPointer = this._bufferPointer;
            }
            else
            {
                for (int i = 0; i < this._bufferPointer; i++)
                {
                    if (this._evaluated[i] == this._buffer[i])
                    {
                        this._evalPointer = i;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        public void Write(LlamaToken token)
        {
            if (this.AvailableBuffer == 0)
            {
                throw new OutOfContextException();
            }

            this._buffer[this._bufferPointer] = token;

            //If the eval pointer is in sync with the buffer (up to date)
            //We need to be up to date because a single mismatch char forces
            //requires an eval for all subsequent tokens.
            if (this._evalPointer >= this._bufferPointer)
            {
                LlamaToken lastEval = this._evaluated[this._bufferPointer];

                if (lastEval.Id != 0 || token.Id == 0) //sanity debug skip. Not needed
                {
                    //Then check to see if the current eval token matches.
                    //If it does, we dont need to eval again
                    if (this._evaluated[this._bufferPointer].Id == token.Id)
                    {
                        //Just in case theres metadata
                        this._evaluated[this._bufferPointer] = token;

                        //Ensure we bump the eval, to keep them in sync
                        this._evalPointer++;
                    }
                }
            }

            this._bufferPointer++;
        }

        private void NativeEval(int[] tokens)
        {
            if (this._evalThreadCount == 0)
            {
                throw new LlamaCppRuntimeError("Evaluation thread count can not be zero");
            }

            if (NativeApi.Eval(this.Handle, tokens, tokens.Length, this._evalPointer, this._evalThreadCount) != 0)
            {
                throw new LlamaCppRuntimeError("Failed to eval.");
            }
        }

        private LlamaTokenCollection NoPenalize()
        {
            LlamaTokenCollection collection = new();
            collection.Append(this.GetToken(13, LlamaTokenType.Undefined)); //NL
            collection.Append(this.GetToken(334, LlamaTokenType.Undefined)); // *
            collection.Append(this.GetToken(29930, LlamaTokenType.Undefined)); //*
            return collection;
        }
    }
}