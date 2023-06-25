using Llama.Collections;
using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Extensions;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Llama.Pipeline.Summarizers
{
    public class ChatSummarizer : IBlockProcessor
    {
        const bool ENABLED = false;

        private readonly int _blocks;
        private readonly int _contextSize;
        private readonly int _blockSize;
        private int _currentBlock = 0;

        private LlamaTokenCollection _currentTokens;
        private LlamaTokenCollection[] _processedTokens;
        private readonly ContextEvaluator _evaluator;
        private readonly IContext _evaluationContext;
        private readonly AutoResetEvent _summarizationGate = new(false);
        public ChatSummarizer(LlamaContextSettings contextSettings, IContext evaluationContext, ContextEvaluator contextEvaluator)
        {
            this._evaluationContext = evaluationContext;
            this._evaluator = contextEvaluator;
            this._blocks = contextSettings.Blocks;
            this._processedTokens = new LlamaTokenCollection[this._blocks];
            this._contextSize = contextSettings.ContextSize;
            this._blockSize = this._contextSize / this._blocks;
            this._currentTokens = new LlamaTokenCollection();
            this._summarizePrefix = this._evaluationContext.Tokenize("USER: Please summarize the following conversation from the perspective of Chie\n", LlamaTokenTags.UNMANAGED);
            this._summarizeSuffix = this._evaluationContext.Tokenize("\nASSISTANT:\n|Chie>  So far we have talked about", LlamaTokenTags.UNMANAGED);
        }
        public IEnumerable<LlamaTokenCollection> Finalize()
        {
            _summarizationGate.WaitOne();

            foreach (LlamaTokenCollection collection in this._processedTokens)
            {
                if(collection is null)
                {
                    continue;
                }

                yield return collection;
            }

            if (this._currentTokens.Count > 0)
            {
                yield return this._currentTokens;
            }

            this._currentTokens = new LlamaTokenCollection();
            this._processedTokens = new LlamaTokenCollection[this._blocks];
            this._currentBlock = 0;
        }

        private void CheckLine()
        {
            if (this._currentTokens.Count > this._blockSize)
            {
                this.ProcessLastBlock(this._currentTokens);
                this._currentTokens = new LlamaTokenCollection();
            }
        }

        private readonly LlamaTokenCollection _summarizePrefix;
        private readonly LlamaTokenCollection _summarizeSuffix;

        private void ProcessLastBlock(LlamaTokenCollection block)
        {
            int thisBlock = this._currentBlock;

            this._currentBlock++;

            if (thisBlock == 0)
            {
                if (ENABLED)
                {
                    LlamaTokenCollection summarized = new();
                    summarized.Append(this._evaluationContext.Tokenize("|Chie> So far we have talked about", LlamaTokenTags.UNMANAGED));

                    LlamaTokenCollection toSummarize = new();
                    toSummarize.Append(LlamaToken.Bos);
                    toSummarize.Append(this._summarizePrefix);
                    toSummarize.Append(block);
                    toSummarize.Append(this._summarizeSuffix);

                    Thread t = new(() =>
                    {
                        foreach (LlamaToken token in this._evaluator.Call(toSummarize))
                        {
                            summarized.Append(token);
                        }

                        this._processedTokens[thisBlock] = summarized;
                        this._evaluationContext.Clear();
                        _summarizationGate.Set();
                    });

                    t.Start();
                } else
                {
                    _summarizationGate.Set();
                }
            }
            else
            {
                this._processedTokens[thisBlock] = this._currentTokens;
            }
        }

        public void Process(ILlamaTokenCollection toSummarize)
        {
            if(toSummarize.Count == 0)
            {
                return;
            }

            foreach (LlamaToken token in toSummarize)
            {
                if(token.Tag == LlamaTokenTags.PROMPT)
                {
                    continue;
                }

                if (token.Tag == LlamaTokenTags.TEMPORARY)
                {
                    continue;
                }

                if(token.Tag == LlamaTokenTags.CONTROL)
                {
                    continue;
                }

                this._currentTokens.Append(token);

                if (token.Id == 13)
                {
                    this.CheckLine();
                }
            }
        }
    }
}
