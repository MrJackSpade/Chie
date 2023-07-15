using Llama.Collections;
using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Extensions;
using Llama.Pipeline.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Llama.Pipeline.Summarizers
{
    public partial class ChatSummarizer : IBlockProcessor
    {

        private const string DIR_SUMMARIZATION = "SummarizationData";

        private const bool ENABLED = true;

        private readonly int _blocks;

        private readonly int _blockSize;

        private readonly int _contextSize;

        private readonly IContext _evaluationContext;

        private readonly ContextEvaluator _evaluator;

        private readonly AutoResetEvent _summarizationGate = new(false);

        private readonly LlamaTokenCollection _summarizePrefix = new();

        private readonly LlamaTokenCollection _summarizeSuffix = new();

        private int _currentBlock = 0;

        private LlamaTokenCollection _currentTokens;

        private LlamaTokenCollection[] _processedTokens;

        public ChatSummarizer(LlamaContextSettings contextSettings, IContext evaluationContext, ContextEvaluator contextEvaluator)
        {
            if (!Directory.Exists(DIR_SUMMARIZATION))
            {
                Directory.CreateDirectory(DIR_SUMMARIZATION);
            }

            this._evaluationContext = evaluationContext;
            this._evaluator = contextEvaluator;
            this._blocks = contextSettings.Blocks;
            this._processedTokens = new LlamaTokenCollection[this._blocks];
            this._contextSize = contextSettings.ContextSize;
            this._blockSize = this._contextSize / this._blocks;
            this._currentTokens = new LlamaTokenCollection(); //Human: Please summarize the above in a single paragraph
            this._summarizePrefix = this.TryTokenize("");
            this._summarizeSuffix = this.TryTokenize("\nHuman: Please summarize the above in a single paragraph\n\nAssistant:");
        }

        public IEnumerable<LlamaTokenCollection> Finalize()
        {
            this._summarizationGate.WaitOne();

            foreach (LlamaTokenCollection collection in this._processedTokens)
            {
                if (collection is null)
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

        private readonly BlockRestriction[] _blockRestrictions = new BlockRestriction[]
        {
            new BlockRestriction()
            {
                Index = 0,
                BanTags =  new string[]
                {
                    LlamaTokenTags.STAGE_DIRECTION,
                    LlamaTokenTags.PROMPT,
                    LlamaTokenTags.CONTROL,
                    LlamaTokenTags.TEMPORARY
                }
            }
        };

        public void Process(ILlamaTokenCollection toSummarize)
        {
            if (toSummarize.Count == 0)
            {
                return;
            }

            BlockRestriction currentBlockRestriction = this._blockRestrictions.Where(b => b.Index == this._currentBlock).SingleOrDefault();

            foreach (LlamaToken token in toSummarize)
            {
                if(currentBlockRestriction is not null && currentBlockRestriction.BanTags.Contains(token.Tag)) 
                { 
                    continue;
                }

                if (this._currentTokens.Count > 1 && this._currentTokens.Last().Id != 13 && token.Value.Contains('|'))
                {
                    this._currentTokens.Append(this._evaluationContext.Tokenize("\n", LlamaTokenTags.UNMANAGED));
                }

                this._currentTokens.Append(token);

                if (token.Id == 13)
                {
                    this.CheckLine();
                }
            }
        }

        private void CheckLine()
        {
            if (this._currentTokens.Count > this._blockSize)
            {
                this.ProcessLastBlock(this._currentTokens);
                this._currentTokens = new LlamaTokenCollection();
            }
        }

        private void Log(string file, LlamaTokenCollection tokens)
        {
            string fName = $"{DateTime.Now.Ticks}.{file}.log";
            string fullName = Path.Combine(DIR_SUMMARIZATION, fName);
            File.WriteAllText(fullName, tokens.ToString());
        }

        private void ProcessLastBlock(LlamaTokenCollection block)
        {
            int thisBlock = this._currentBlock;

            this._currentBlock++;

            if (thisBlock == 0)
            {
                if (ENABLED)
                {
                    LlamaTokenCollection summarized = new();

                    summarized.Append(this._evaluationContext.Tokenize("|Chie: ", LlamaTokenTags.UNMANAGED));

                    LlamaTokenCollection toSummarize = new();

                    //toSummarize.Append(LlamaToken.Bos);

                    if (this._summarizePrefix.Count > 0)
                    {
                        toSummarize.Append(this._summarizePrefix);
                    }

                    toSummarize.Append(block);

                    if (this._summarizeSuffix.Count > 0)
                    {
                        toSummarize.Append(this._summarizeSuffix);
                    }

                    this.Log("ToSummarize", toSummarize);

                    Thread t = new(() =>
                    {
                        foreach (LlamaToken token in this._evaluator.Call(toSummarize))
                        {
                            summarized.Append(token);
                        }

                        this.Log("Summarized", summarized);

                        LlamaTokenCollection[] summaryArray = summarized.Split(13).Where(s => s.Count > 0).ToArray();

                        LlamaTokenCollection cleaned = new();

                        for (int i = 0; i < summaryArray.Length - 1; i++)
                        {
                            if (i > 0)
                            {
                                cleaned.Append(this._evaluationContext.Tokenize(" ", LlamaTokenTags.INPUT));
                            }

                            cleaned.Append(summaryArray[i]);
                        }

                        this.Log("SummarizedCleaned", cleaned);

                        this._processedTokens[thisBlock] = cleaned;
                        this._evaluationContext.Clear();
                        this._summarizationGate.Set();
                    });

                    t.Start();
                }
                else
                {
                    this._summarizationGate.Set();
                }
            }
            else
            {
                this._processedTokens[thisBlock] = this._currentTokens;
            }
        }

        private LlamaTokenCollection TryTokenize(string toTokenize)
        {
            if (string.IsNullOrWhiteSpace(toTokenize))
            {
                return new LlamaTokenCollection();
            }

            return this._evaluationContext.Tokenize(toTokenize, LlamaTokenTags.UNMANAGED);
        }
    }
}