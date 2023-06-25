using Llama.Collections;
using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Context.Samplers;
using Llama.Context.Samplers.Interfaces;
using Llama.Data;
using Llama.Events;
using Llama.Exceptions;
using Llama.Extensions;
using Llama.Model;
using Llama.Native;
using Llama.Native.Data;
using Llama.Pipeline.Interfaces;
using Llama.Scheduler;
using Llama.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Llama.Context
{
    public class LlamaContextWrapper : IContext
    {
        private readonly LlamaTokenCollection _buffer;

        private readonly IContextRoller _contextRoller;

        private readonly int _evalThreadCount;

        private readonly LlamaTokenCollection _evaluated;

        private readonly IFinalSampler _finalSampler;

        private readonly IList<IPostResponseContextTransformer> _postResponseTransforms;

        private readonly LlamaTokenCollection _prompt;

        private readonly LlamaContextSettings _settings;

        private readonly IList<ISimpleSampler> _simpleSamplers;

        private readonly IList<ITokenTransformer> _tokenTransformers;

        private readonly IExecutionScheduler _executionScheduler;

        private int _bufferPointer = 0;

        private int _evalPointer = 0;

        public LlamaContextWrapper(IExecutionScheduler executionScheduler, ITextSanitizer textSanitizer, SafeLlamaContextHandle handle, LlamaModelSettings modelSettings, LlamaContextSettings settings, IEnumerable<IPostResponseContextTransformer> postResponseTransforms, IEnumerable<ITokenTransformer> tokenTransformers, IEnumerable<ISimpleSampler> simpleSamplers, IFinalSampler finalSampler, IContextRoller contextRoller)
        {
            if (executionScheduler is null)
            {
                throw new ArgumentNullException(nameof(executionScheduler));
            }

            if (handle is null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (modelSettings is null)
            {
                throw new ArgumentNullException(nameof(modelSettings));
            }

            if (postResponseTransforms is null)
            {
                throw new ArgumentNullException(nameof(postResponseTransforms));
            }

            if (tokenTransformers is null)
            {
                throw new ArgumentNullException(nameof(tokenTransformers));
            }

            if (simpleSamplers is null)
            {
                throw new ArgumentNullException(nameof(simpleSamplers));
            }

            if (contextRoller is null)
            {
                throw new ArgumentNullException(nameof(contextRoller));
            }

            this._executionScheduler = executionScheduler;
            this._contextRoller = contextRoller;
            this.Handle = handle;
            this._tokenTransformers = tokenTransformers.ToList();
            this._postResponseTransforms = postResponseTransforms.ToList();
            this._simpleSamplers = simpleSamplers.ToList();
            this._finalSampler = finalSampler ?? throw new ArgumentNullException(nameof(finalSampler));
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this._evalThreadCount = modelSettings.ThreadCount;
            this.Size = this._settings.ContextSize;
            this._buffer = new LlamaTokenBuffer(this.Size);
            this._buffer[0] = LlamaToken.Bos;
            this._evaluated = new LlamaTokenBuffer(this.Size);

            this._prompt = this.Tokenize(textSanitizer.Sanitize(settings.Prompt), LlamaTokenTags.PROMPT);
        }

        protected LlamaContextWrapper()
        {
        }

        event EventHandler<ContextModificationEventArgs> IContext.OnContextModification
        {
            add => OnContextModification += value;
            remove => OnContextModification -= value;
        }

        public event EventHandler<ContextModificationEventArgs> OnContextModification;

        public int AvailableBuffer => this.Size - this._bufferPointer;

        public IReadOnlyLlamaTokenCollection Buffer => this._buffer;

        public Encoding Encoding => this._settings.Encoding;

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

        private void NativeEval(int[] tokens)
        {
            if (NativeApi.Eval(this.Handle, tokens, tokens.Length, this._evalPointer, this._evalThreadCount) != 0)
            {
                LlamaLogger.Default.Error($"Failed to eval.");
                throw new RuntimeError("Failed to eval.");
            }
        }

        public int Evaluate(int count = -1)
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

                this.TriggerModificationEvent(this._evalPointer, n_eval);

                try
                {
                    Debug.WriteLine($"{this._evalPointer + 1}/{end}");

                    this._executionScheduler.Execute(() => this.NativeEval(tokens), this._settings.ExecutionPriority);
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

                this.TriggerModificationEvent(this._evalPointer);
            }

            for (int i = this._evalPointer; i < this.Evaluated.Count; i++)
            {
                this._evaluated[i] = LlamaToken.Null;
            }

            this.TriggerModificationEvent();

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

        public void PostProcess()
        {
            IEnumerable<LlamaToken> contextBuffer = this.Evaluated;

            LlamaTokenCollection checkMe = new(contextBuffer.Where(t => t.Id != 0));

            checkMe.Ensure();

            if (this._postResponseTransforms.Count == 0)
            {
                return;
            }

            LlamaTokenCollection postEvaluationTransform = new(this._postResponseTransforms.Transform(contextBuffer));

            postEvaluationTransform.Ensure();

            this.SetBuffer(postEvaluationTransform);

            this.Evaluate();
        }

        public SampleResult SampleNext(IReadOnlyLlamaTokenCollection thisCall) => this._executionScheduler.Execute(() => this.SampleNextInternal(thisCall), this._settings.ExecutionPriority);

        private SampleResult SampleNextInternal(IReadOnlyLlamaTokenCollection thisCall)
        {
            LlamaTokenCollection selectedCollection;
            List<LlamaToken> selectedTokens;

            do
            {
                LlamaToken token = this.SampleTokenRaw(thisCall);

                selectedTokens = this._tokenTransformers.Transform(this._settings, thisCall, this, token).ToList();

                selectedCollection = new(selectedTokens);
            } while (thisCall.IsNullOrWhiteSpace && selectedCollection.IsNullOrWhiteSpace || selectedCollection.IsNullOrEmpty);

            LlamaTokenCollection appendedCall = new(thisCall);
            appendedCall.Append(selectedCollection);

            return new()
            {
                Tokens = selectedCollection,
                IsFinal = this.ShouldBreak(appendedCall)
            };
        }

        public LlamaToken SampleTokenRaw(IReadOnlyLlamaTokenCollection thisCall)
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
                ContextTokens = Evaluated,
                InferrenceTokens = thisCall,
                Logits = logits
            };

            foreach (ISimpleSampler simpleSampler in this._simpleSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            logits.Update(no_penalize);

            int tokenId = this._finalSampler.SampleNext(sampleContext);

            return this.GetToken(tokenId, LlamaTokenTags.RESPONSE);
        }

        public void Write(LlamaToken token)
        {
            // infinite text generation via context swapping
            // if we run out of context:
            // - take the n_keep first tokens from the original prompt (via n_past)
            // - take half of the last (n_ctx - n_keep) tokens and recompute the logits in batches
            if (this.AvailableBuffer == 0)
            {
                LlamaTokenCollection newContext = this._contextRoller.GenerateContext(this, this._prompt, this._settings.KeepContextTokenCount);

                this.SetBuffer(newContext);
            }

            if (this._bufferPointer == 0 && token.Id != NativeApi.TokenBos())
            {
                throw new Exception("First token must be BOS");
            }

            if (this._bufferPointer > 0 && token.Id == NativeApi.TokenBos())
            {
                Debugger.Break();
            }

            this._buffer[this._bufferPointer] = token;

            //If the eval pointer is in sync with the buffer (up to date)
            //We need to be up to date because a single mismatch char forces
            //requires an eval for all subsequent tokens.
            if (this._evalPointer == this._bufferPointer)
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

            string bufferString = this._buffer.Trim().ToString();

            if(bufferString.Contains("USER:"))
            {
                //Debugger.Break();
            }

            this.TriggerModificationEvent();
        }

        private LlamaTokenCollection NoPenalize()
        {
            LlamaTokenCollection collection = new();
            collection.Append(this.GetToken(13, LlamaTokenTags.UNMANAGED)); //NL
            collection.Append(this.GetToken(334, LlamaTokenTags.UNMANAGED)); // *
            collection.Append(this.GetToken(29930, LlamaTokenTags.UNMANAGED)); //*
            return collection;
        }

        private bool ShouldBreak(LlamaTokenCollection toTest)
        {
            string returnValue = toTest.ToString();

            foreach (string antiprompt in this._settings.Antiprompt)
            {
                if (returnValue.EndsWith(antiprompt))
                {
                    return true;
                }
            }

            if (toTest.Contains(NativeApi.TokenEos()))
            {
                return true;
            }

            if (this._settings.PredictCount > 0 && toTest.Count >= this._settings.PredictCount)
            {
                return true;
            }

            return false;
        }

        private void TriggerModificationEvent(int evalIndex = -1, int evalCount = -1)
        {
            do
            {
                try
                {
                    OnContextModification?.Invoke(this, new ContextModificationEventArgs(this._evaluated, this._buffer, this._evalPointer, evalIndex, evalCount));
                    return;
                }
                catch (Exception e)
                {

                }
            } while (true);
        }
    }
}