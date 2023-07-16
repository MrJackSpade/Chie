using Llama.Collections;
using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context.Extensions;
using Llama.Data;
using Llama.Extensions;
using Llama.Pipeline.Interfaces;
using LlamaApiClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Llama.Pipeline.Summarizers
{
    public partial class ChatSummarizer : IBlockProcessor
    {

        private const string DIR_SUMMARIZATION = "SummarizationData";

        private const bool ENABLED = true;

        private readonly int _blocks;

        private readonly int _blockSize;

        private readonly int _contextSize;

        private readonly AutoResetEvent _summarizationGate = new(false);

        private readonly string _summarizePrefix = string.Empty;

        private readonly string _summarizeSuffix = string.Empty;

        private int _currentBlock = 0;

        private LlamaTokenCollection _currentTokens;

        private LlamaTokenCollection[] _processedTokens;

        private readonly LlamaClient _client = new(new LlamaClientSettings("http://192.168.0.93"));

        private Guid _contextId;

        private bool _isSetup;

        private async Task TrySetup()
        {
            if (!this._isSetup)
            {
                await this._client.LoadModel(new LlamaModelSettings()
                {
                    Model = "D:\\Chie\\Models\\airoboros-65b-gpt4-1.3.ggmlv3.q5_K_M.bin",
                    ContextSize = 2048,
                    BatchSize = 512
                });

                this._contextId = (await this._client.LoadContext(new LlamaContextSettings()
                {
                    BatchSize = 512,
                    ContextSize = 2048,
                    EvalThreadCount = 8
                }, (cr) =>
                {
                    cr.MirostatSamplerSettings = new LlamaApi.Models.Request.MirostatSamplerSettings()
                    {
                        MirostatType = LlamaApi.Models.Response.MirostatType.One
                    };
                    cr.RepetitionSamplerSettings = new Core.Samplers.Repetition.RepetitionSamplerSettings();
                }
                )).Id;

                await this._client.Tokenize(this._contextId, " ");

                this._isSetup = true;
            }
        }
        public ChatSummarizer(Context.LlamaContextSettings contextSettings)
        {
            if (!Directory.Exists(DIR_SUMMARIZATION))
            {
                Directory.CreateDirectory(DIR_SUMMARIZATION);
            }

            this._summarizeSuffix = "\nHuman: Please summarize the above in a single paragraph\n\nAssistant:";

            this._blocks = contextSettings.Blocks;
            this._processedTokens = new LlamaTokenCollection[this._blocks];
            this._contextSize = contextSettings.ContextSize;
            this._blockSize = this._contextSize / this._blocks;
            this._currentTokens = new LlamaTokenCollection(); //Human: Please summarize the above in a single paragraph
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

        public async Task Process(ILlamaTokenCollection toSummarize)
        {
            await this.TrySetup();

            if (toSummarize.Count == 0)
            {
                return;
            }

            BlockRestriction currentBlockRestriction = this._blockRestrictions.Where(b => b.Index == this._currentBlock).SingleOrDefault();

            foreach (LlamaToken token in toSummarize)
            {
                if (currentBlockRestriction is not null && currentBlockRestriction.BanTags.Contains(token.Tag))
                {
                    continue;
                }

                if (this._currentTokens.Count > 1 && this._currentTokens.Last().Id != 13 && token.Value.Contains('|'))
                {
                    this._currentTokens.Append(LlamaToken.NewLine);
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

        private async Task<IReadOnlyLlamaTokenCollection> RemoteTokenize(string toTokenize)
        {
            int[] tokens = await this._client.Tokenize(this._contextId, toTokenize);

            LlamaTokenCollection toReturn = new();

            foreach (int t in tokens)
            {
                toReturn.Append(new LlamaToken(t, IntPtr.Zero, ""));
            }

            return toReturn;
        }
        private async void ProcessLastBlock(LlamaTokenCollection block)
        {
            int thisBlock = this._currentBlock;

            this._currentBlock++;

            if (thisBlock == 0)
            {
                if (ENABLED)
                {
                    LlamaTokenCollection summarized = new();

                    summarized.Append(await this.RemoteTokenize("|Chie: "));

                    StringBuilder toSummarize = new();
        
                    toSummarize.Append(this._summarizePrefix);
                   
                    toSummarize.Append(block);
                 
                    toSummarize.Append(this._summarizeSuffix);
                    
                    Thread t = new(async () =>
                    {
                        await this._client.Write(this._contextId, new LlamaApi.Models.Request.RequestLlamaToken()
                        {
                            TokenId = LlamaToken.BOS.Id
                        }, startIndex: 0);

                        await this._client.Write(this._contextId, toSummarize.ToString());

                        await this._client.Eval(this._contextId);

                        InferenceEnumerator enumerator = await this._client.Infer(this._contextId);

                        while (await enumerator.MoveNextAsync())
                        {
                            summarized.Append(new LlamaToken(enumerator.Current.Id, enumerator.Current.Value, ""));
                        }

                        LlamaTokenCollection[] summaryArray = summarized.Split(13).Where(s => s.Count > 0).ToArray();

                        LlamaTokenCollection cleaned = new();

                        int space = (await this._client.Tokenize(this._contextId, " "))[0];

                        LlamaToken spaceToken = new(space, " ", "");

                        for (int i = 0; i < summaryArray.Length - 1; i++)
                        {
                            if (i > 0)
                            {
                                cleaned.Append(spaceToken);
                            }

                            cleaned.Append(summaryArray[i]);
                        }

                        this.Log("SummarizedCleaned", cleaned);

                        this._processedTokens[thisBlock] = cleaned;
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
    }
}