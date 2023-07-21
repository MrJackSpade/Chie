using Ai.Abstractions;
using Ai.Utils.Extensions;
using ChieApi.Extensions;
using ChieApi.Extensions.Llama.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Samplers;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using ChieApi.TokenTransformers;
using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApiClient;
using Loxifi;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
    public class LlamaService
    {
        private readonly AutoResetEvent _acceptingInput = new(false);

        private readonly ICharacterFactory _characterFactory;

        private readonly SemaphoreSlim _chatLock = new(1);

        private readonly ChatService _chatService;

        private readonly LlamaContextClient _client ;

        private readonly LlamaContextModel _contextModel;

        private readonly ILogger _logger;

        private readonly LogitService _logitService;

        private readonly AutoResetEvent _processingInput = new(false);

        private readonly List<ISimpleSampler> _simpleSamplers;

        private readonly LlamaTokenCache _tokenCache;

        private readonly List<ITokenTransformer> _transformers;

        private CharacterConfiguration _characterConfiguration;

        public LlamaService(ICharacterFactory characterFactory, LlamaContextClient client, LlamaContextModel contextModel, LlamaTokenCache llamaTokenCache, ILogger logger, ChatService chatService, LogitService logitService)
        {
            this._client = client;
            this._characterFactory = characterFactory;
            this._logitService = logitService;
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            this._contextModel = contextModel;
            this._tokenCache = llamaTokenCache;
            logger.LogInformation("Constructing Llama Service");

            _ = this.SetState(AiState.Initializing);

            this._simpleSamplers = new List<ISimpleSampler>()
            {
                new NewlineEnsureSampler(llamaTokenCache)
            };

            this._transformers = new List<ITokenTransformer>()
            {
                new TextTruncationTransformer(250, 150, ".!?", llamaTokenCache)
            };

            this.Initialization = Task.Run(this.Init);
        }

        public AiState AiState { get; private set; }

        public string CharacterName { get; private set; }

        public IAsyncEnumerable<LlamaToken> Context => _contextModel;

        public string CurrentResponse { get; private set; }

        public Task Initialization { get; private set; }

        public Thread LoopingThread { get; private set; }

        private string LastChannel { get; set; }

        private long LastMessageId { get; set; }

        public LlamaClientResponseState CheckIfResponding(string channel)
        {
            LlamaClientResponseState responseState = new()
            {
                IsTyping = this.LastChannel == channel && this.AiState == AiState.Responding
            };

            if (responseState.IsTyping)
            {
                responseState.Content = this.CurrentResponse;
            }

            return responseState;
        }

        public ChatEntry[] GetResponses(string channelId, long after) => this._chatService.GetMessages(channelId, after, this.CharacterName);

        public long ReturnControl(bool force, string? channelId = null)
        {
            bool channelAllowed = channelId == null || this.LastChannel == channelId;
            bool clearToProceed = channelAllowed && (force || this._acceptingInput.WaitOne(100));

            long lastMessageId = this.LastMessageId;

            if (!clearToProceed)
            {
                this._acceptingInput.Set();
            }
            else
            {
                this._processingInput.Set();
            }

            return lastMessageId;
        }

        public async Task<long> Send(ChatEntry chatEntry) => await this.Send(new ChatEntry[] { chatEntry });

        public async Task<long> Send(ChatEntry[] chatEntries)
        {
            await this.Initialization;

            try
            {
                if (!this.TryLock())
                {
                    this._logger.LogWarning($"Client not idle. Skipping ({chatEntries.Length}) messages.");
                    return 0;
                }

                this._logger.LogInformation("Sending messages to client...");

                LlamaSafeString[] cleanedMessages = chatEntries.Select(LlamaSafeString.Parse).ToArray();

                if (cleanedMessages.Length != 0)
                {
                    for (int i = 0; i < chatEntries.Length; i++)
                    {
                        bool last = i == chatEntries.Length - 1;

                        LlamaSafeString cleanedMessage = cleanedMessages[i];

                        await this.SendText(chatEntries[i], last);
                    }
                }

                this._logger.LogInformation($"Last Message Id: {this.LastMessageId}");

                return this.LastMessageId;
            }
            finally
            {
                this.Unlock();
            }
        }

        public bool TryGetReply(long originalMessageId, out ChatEntry? chatEntry) => this._chatService.TryGetOriginal(originalMessageId, out chatEntry);

        private async Task<LlamaTokenCollection> Infer()
        {
            InferenceEnumerator enumerator = await this._client.Infer();

            LlamaTokenCollection thisInference = new();

            enumerator.SetLogit(LlamaToken.EOS.Id, 0, LogitBiasLifeTime.Temporary);

            while (await enumerator.MoveNextAsync())
            {
                await foreach (LlamaToken llamaToken in this._transformers.Transform(thisInference, new LlamaToken(enumerator.Current.Id, enumerator.Current.Value)))
                {
                    if (llamaToken.Id == 13)
                    {
                        return thisInference;
                    }

                    thisInference.Append(llamaToken);

                    Dictionary<int, float> logits = await this._simpleSamplers.SampleNext(thisInference);

                    foreach(KeyValuePair<int, float> pair in logits)
                    {
                        enumerator.SetLogit(pair.Key, pair.Value, LogitBiasLifeTime.Temporary);
                    }
                }
            }

            return thisInference;
        }

        private async Task Init()
        {
            this._logger.LogInformation("Constructing Llama Client");
            this._characterConfiguration ??= await this._characterFactory.Build();
            this.CharacterName = this._characterConfiguration.CharacterName;

            this._logger.LogDebug(System.Text.Json.JsonSerializer.Serialize(this._characterConfiguration, new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            }));

            this._logger.LogInformation("Connecting to client...");

            await this._client.LoadModel(new LlamaModelSettings()
            {
                BatchSize = this._characterConfiguration.BatchSize,
                ContextSize = this._characterConfiguration.ContextLength,
                GpuLayerCount = this._characterConfiguration.GpuLayers,
                MemoryMode = this._characterConfiguration.MemoryMode,
                Model = this._characterConfiguration.ModelPath,
                ThreadCount = this._characterConfiguration.Threads ?? System.Environment.ProcessorCount / 2,
                UseMemoryMap = !this._characterConfiguration.NoMemoryMap,
                UseMemoryLock = true
            });

            await this._client.LoadContext(new LlamaContextSettings()
            {
                BatchSize = this._characterConfiguration.BatchSize,
                ContextSize = this._characterConfiguration.ContextLength,
                EvalThreadCount = this._characterConfiguration.Threads ?? System.Environment.ProcessorCount / 2
            }, (c) =>
            {
                c.ContextId = Guid.Empty;

                if(this._characterConfiguration.MiroStat != MirostatType.None)
                {
                    c.MirostatSamplerSettings = new MirostatSamplerSettings()
                    {
                        Tau = this._characterConfiguration.MiroStatEntropy,
                        Temperature = this._characterConfiguration.Temperature,
                        MirostatType = this._characterConfiguration.MiroStat
                    };
                } else
                {
                    c.TemperatureSamplerSettings = new TemperatureSamplerSettings()
                    {
                        Temperature = this._characterConfiguration.Temperature,
                        TopP = this._characterConfiguration.TopP,
                    };
                }

                if(this._characterConfiguration.RepeatPenalty != 0)
                {
                    c.RepetitionSamplerSettings = new RepetitionSamplerSettings()
                    {
                        RepeatPenalty = this._characterConfiguration.RepeatPenalty,
                        RepeatTokenPenaltyWindow = this._characterConfiguration.RepeatPenaltyWindow,
                    };
                }
            });

            this._logger.LogInformation("Connected to client.");

            if (this.AiState == AiState.Initializing)
            {
                _ = await this.SetState(AiState.Idle);
            }

            this.LoopingThread = new Thread(async () => await this.LoopProcess());
            this.LoopingThread.Start();
        }

        private async Task LoopProcess()
        {
            do
            {
                this._acceptingInput.Set();
                this._processingInput.WaitOne();

                LlamaTokenCollection contextState = new();

                await foreach (LlamaToken token in this._contextModel)
                {
                    await this.SetState(AiState.Responding);

                    contextState.Append(token);
                }

                contextState.Append(await _tokenCache.Get($"|{this.CharacterName}:"));

                await this._client.Write(contextState, 0);

                await this._client.Eval();

                LlamaTokenCollection thisInference = await this.Infer();

                await this.RecieveResponse(thisInference);
            } while (true);
        }

        private async Task RecieveResponse(LlamaTokenCollection collection)
        {
            foreach (LlamaToken token in collection)
            {
                this._logitService.Identify(token.Id, token.Value, token.GetEscapedValue());
            }

            string responseContent = collection.ToString();

            string? userName = this.CharacterName;
            string? content = responseContent.To("|")!.Trim();
            this.CurrentResponse = string.Empty;

            if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(content))
            {
                this._logger.LogWarning("Empty message returned. Ignoring...");
                return;
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = this.CharacterName;
            }

            ChatEntry chatEntry = new()
            {
                ReplyToId = LastMessageId,
                Content = content,
                DisplayName = userName,
                SourceChannel = LastChannel
            };

            _contextModel.Messages.Enqueue(new LlamaMessage(this.CharacterName, collection, LlamaTokenType.Response, this._tokenCache));

            _ = await this.SetState(AiState.Idle);

            if (chatEntry.Content != null)
            {
                _ = this._chatService.Save(chatEntry);
            }
        }

        private async Task SendText(ChatEntry chatEntry, bool flush)
        {
            await this.Initialization;

            _ = await this.SetState(AiState.Processing);

            this.LastMessageId = await this._chatService.Save(chatEntry);
            this.LastChannel = chatEntry.SourceChannel;

            string toSend;

            if (!string.IsNullOrWhiteSpace(chatEntry.DisplayName))
            {
                this._contextModel.Messages.Enqueue(new LlamaMessage(chatEntry.DisplayName, chatEntry.Content, LlamaTokenType.Input, this._tokenCache));
            }
            else
            {
                this._contextModel.Messages.Enqueue(new LlamaTokenBlock(chatEntry.Content, LlamaTokenType.Input, this._tokenCache));
            }

            if (flush)
            {
                this.ReturnControl(true);
            }
        }

        private async Task<bool> SetState(AiState state)
        {
            if (this.AiState != state)
            {
                this._logger.LogInformation("Setting client state: " + state.ToString());
                this.AiState = state;
                return true;
            }

            return false;
        }

        private bool TryLock()
        {
            this._chatLock.Wait();

            if (this.AiState != AiState.Idle)
            {
                return false;
            }

            return true;
        }

        private void Unlock() => _ = this._chatLock.Release();
    }
}