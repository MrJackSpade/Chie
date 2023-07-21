using ChieApi.Interfaces;
using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using LlamaApiClient;
using System.Text;

namespace ChieApi.Services
{
    public class SummaryResponse
    {
        public SummaryResponse(LlamaTokenCollection summary, IReadOnlyList<ITokenCollection> summarized)
        {
            this.Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            this.Summarized = summarized ?? throw new ArgumentNullException(nameof(summarized));
        }

        public LlamaTokenCollection Summary { get; private set; }
        public IReadOnlyList<ITokenCollection> Summarized { get; private set; }
    }
    public partial class SummarizationService
    {
        private const string DIR_SUMMARIZATION = "SummarizationData";

        private readonly int _contextSize;

        private readonly string _summarizePrefix = string.Empty;

        private readonly string _summarizeSuffix = string.Empty;

        private readonly LlamaClient _client = new(new LlamaClientSettings("http://192.168.0.93"));

        private readonly Guid _contextId = Guid.Parse("1dbfcbcd-153f-4f11-9a1d-b309da9f06cb");

        private bool _isSetup;

        public async Task TrySetup()
        {
            if (!this._isSetup)
            {
                await this._client.LoadModel(new LlamaModelSettings()
                {
                    Model = "D:\\Chie\\Models\\airoboros-65b-gpt4-1.3.ggmlv3.q5_K_M.bin",
                    ContextSize = 2048,
                    BatchSize = 512
                });

                _ = (await this._client.LoadContext(new LlamaContextSettings()
                {
                    BatchSize = 512,
                    ContextSize = 2048,
                    EvalThreadCount = 8,
                }, (cr) =>
                {
                    cr.ContextId = this._contextId;
                    cr.TemperatureSamplerSettings = new TemperatureSamplerSettings()
                    {
                        Temperature = -1
                    };

                    cr.RepetitionSamplerSettings = new RepetitionSamplerSettings();
                }
                )).Id;

                this._isSetup = true;
            }
        }
        public SummarizationService(LlamaContextSettings contextSettings)
        {
            if (!Directory.Exists(DIR_SUMMARIZATION))
            {
                Directory.CreateDirectory(DIR_SUMMARIZATION);
            }

            this._summarizeSuffix = "\nHuman: Please summarize the above in a single paragraph\n\nAssistant:";

            this._contextSize = contextSettings.ContextSize;
        }

        private async Task<IReadOnlyLlamaTokenCollection> RemoteTokenize(string toTokenize) => await this._client.Tokenize(this._contextId, toTokenize);

        public async Task<SummaryResponse> Summarize(IList<ITokenCollection> block)
        {

            TaskCompletionSource<LlamaTokenCollection> toReturn = new();

            LlamaTokenCollection summarized = new();

            await summarized.Append(this.RemoteTokenize("["));

            StringBuilder toSummarize = new();

            toSummarize.Append(this._summarizePrefix);

            foreach (ITokenCollection collection in block)
            {
                StringBuilder thisCollection = new();

                await foreach (LlamaToken lt in collection)
                {
                    thisCollection.Append(lt.Value);
                }

                toSummarize.AppendLine(thisCollection.ToString());
            }

            toSummarize.Append(this._summarizeSuffix);

            await this._client.Write(this._contextId, new LlamaApi.Models.Request.RequestLlamaToken()
            {
                TokenId = LlamaToken.BOS.Id
            }, startIndex: 0);

            await this._client.Write(this._contextId, toSummarize.ToString());

            await this._client.Eval(this._contextId);

            InferenceEnumerator enumerator = await this._client.Infer(this._contextId);

            enumerator.SetLogit(2, 0, LogitBiasLifeTime.Temporary);

            Queue<int> tokenIds = new();

            while (await enumerator.MoveNextAsync())
            {
                tokenIds.Enqueue(enumerator.Current.Id);

                List<int> distinctTokens = tokenIds.Distinct().ToList();

                if (tokenIds.Count > 2 && distinctTokens.Count == 1)
                {
                    enumerator.MoveBack();
                    enumerator.SetLogit(distinctTokens.Single(), 0, LogitBiasLifeTime.Temporary);
                }
                else
                {
                    summarized.Append(new LlamaToken(enumerator.Current.Id, enumerator.Current.Value));
                    Console.Write(enumerator.Current.Value);
                }

                if (tokenIds.Count > 3)
                {
                    tokenIds.Dequeue();
                }
            }

            summarized.Append(await this.RemoteTokenize("]"));

            LlamaTokenCollection[] summaryArray = summarized.Split(13).Where(s => s.Count > 0).ToArray();

            LlamaTokenCollection cleaned = new();

            LlamaToken spaceToken = (await this.RemoteTokenize(" ")).First();

            for (int i = 0; i < summaryArray.Length; i++)
            {
                if (i > 0)
                {
                    cleaned.Append(spaceToken);
                }

                cleaned.Append(summaryArray[i]);
            }

            return new SummaryResponse(cleaned, block.ToList());
        }
    }
}