using Ai.Abstractions;
using Ai.Utils.Extensions;
using ChieApi.CleanupPipeline;
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
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using LlamaApi.Models.Request;
using LlamaApi.Shared.Models.Response;
using LlamaApiClient;
using Loxifi;
using Loxifi.AsyncExtensions;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
    public class LlamaService
    {
        private readonly AutoResetEvent _acceptingInput = new(false);

        private readonly ICharacterFactory _characterFactory;

        private readonly SemaphoreSlim _chatLock = new(1);

        private readonly ChatService _chatService;

        private readonly LlamaContextClient _client;

        private readonly ILogger _logger;

        private readonly LogitService _logitService;

        private readonly AutoResetEvent _processingInput = new(false);

        private readonly List<ISimpleSampler> _simpleSamplers;

        private readonly List<IResponseCleaner> _responseCleaners;

        private readonly SummarizationService _summarizationService;

        private readonly LlamaTokenCache _tokenCache;

        private readonly List<ITokenTransformer> _transformers;

        private CharacterConfiguration _characterConfiguration;

        private LlamaTokenCollection _context;

        private LlamaContextModel _contextModel;

        private readonly DictionaryService _dictionaryService;

        private Task<SummaryResponse>? _summaryTask;

        public LlamaService(SummarizationService summarizationService, DictionaryService dictionaryService, ICharacterFactory characterFactory, LlamaContextClient client, LlamaContextModel contextModel, LlamaTokenCache llamaTokenCache, ILogger logger, ChatService chatService, LogitService logitService)
        {
            this._dictionaryService = dictionaryService;
            this._summarizationService = summarizationService;
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
                new NewlineEnsureSampler(llamaTokenCache),
                new RepetitionBlockingSampler(3)
            };

            this._transformers = new List<ITokenTransformer>()
            {
                new SpaceStartTransformer(),
                new NewlineTransformer(),
                new TextTruncationTransformer(1000, 250, 150, ".!?", llamaTokenCache),
                new RepetitionBlockingTransformer(3),
                new InvalidCharacterBlockingTransformer()
            };

            this._responseCleaners = new List<IResponseCleaner>()
            {
                new SpellingCleaner(dictionaryService),
                new DanglingQuoteCleaner()
            };

            this.Initialization = Task.Run(this.Init);
        }

        public AiState AiState { get; private set; }

        public string CharacterName { get; private set; }

        public IReadOnlyLlamaTokenCollection Context => this._context;

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

        public async Task Save()
        {
            string fname = $"State.{DateTime.Now.Ticks}.json";
            string fPath = Path.Combine("ContextStates", fname);
            FileInfo saveInfo = new(fPath);
            if (!saveInfo.Directory.Exists)
            {
                saveInfo.Directory.Create();
            }

            ContextSaveState contextSaveState = new();
            await contextSaveState.LoadFrom(this._contextModel);
            contextSaveState.SaveTo(saveInfo.FullName);
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

        public bool TryLoad()
        {
            if (Directory.Exists("ContextStates"))
            {
                IEnumerable<string> files = Directory.EnumerateFiles("ContextStates", "State.*.json");
                IEnumerable<FileInfo> fileInfoes = files.Select(f => new FileInfo(f));
                IEnumerable<FileInfo> orderedFileInfoes = fileInfoes.OrderByDescending(d => d.LastWriteTime);
                FileInfo? lastState = orderedFileInfoes.FirstOrDefault();

                if (lastState != null)
                {
                    ContextSaveState contextSaveState = new();
                    contextSaveState.LoadFrom(lastState.FullName);
                    this._contextModel = contextSaveState.ToModel(this._tokenCache);
                    return true;
                }
            }

            return false;
        }

        private async Task Cleanup()
        {
            LlamaTokenCollection contextState = await this._contextModel.GetState();

            this._context = contextState;

            if (contextState.Count > this._characterConfiguration.ContextLength)
            {
                Debugger.Break();
                throw new Exception("Context length too long?");
            }

            await this._client.Eval(contextState, 0);

            this.CurrentResponse = string.Empty;
        }

        private async Task<IReadOnlyLlamaTokenCollection> Infer()
        {
            InferenceEnumerator enumerator = this._client.Infer();

            enumerator.SetLogit(LlamaToken.EOS.Id, 0, LogitBiasLifeTime.Temporary);
            enumerator.SetLogit(LlamaToken.NewLine.Id, 0, LogitBiasLifeTime.Temporary);

            enumerator.SetLogits(this._characterConfiguration.LogitOverrides, LogitBiasLifeTime.Inferrence);

            while (await enumerator.MoveNextAsync())
            {
                this.SetState(AiState.Responding);

                LlamaToken selected = new(enumerator.Current.Id, enumerator.Current.Value);

                await foreach (LlamaToken llamaToken in this._transformers.Transform(enumerator, selected))
                {
                    //Neither of these need to be accepted because the local
                    //context manages both
                    if (llamaToken.Id == LlamaToken.EOS.Id)
                    {
                        return enumerator.Enumerated;
                    }

                    if (llamaToken.Value != null)
                    {
                        Console.Write(llamaToken.Value);
                    }

                    await enumerator.Accept(llamaToken);

                    this.CurrentResponse += llamaToken.Value;

                    this._context.Append(llamaToken);
                }

                if (!enumerator.Accepted)
                {
                    enumerator.MoveBack();
                }

                await this._simpleSamplers.SampleNext(enumerator);
            }

            return enumerator.Enumerated.TrimWhiteSpace();
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
                UseGqa = this._characterConfiguration.UseGqa,
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
                EvalThreadCount = this._characterConfiguration.Threads ?? System.Environment.ProcessorCount / 2,
            }, (c) =>
            {
                c.ContextId = Guid.Empty;

                if (this._characterConfiguration.MiroStat != MirostatType.None)
                {
                    c.MirostatSamplerSettings = new MirostatSamplerSettings()
                    {
                        Tau = this._characterConfiguration.MiroStatEntropy,
                        Temperature = this._characterConfiguration.Temperature,
                        MirostatType = this._characterConfiguration.MiroStat
                    };
                }
                else
                {
                    c.TemperatureSamplerSettings = new TemperatureSamplerSettings()
                    {
                        Temperature = this._characterConfiguration.Temperature,
                        TopP = this._characterConfiguration.TopP,
                    };
                }

                if (this._characterConfiguration.RepeatPenalty != 0)
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
                _ = this.SetState(AiState.Idle);
            }

            if (!this.TryLoad())
            {
                Console.WriteLine("No state found... Prompting...");

                foreach (string s in FileService.GetStringOrContent(this._characterConfiguration.Start).CleanSplit())
                {
                    string? displayName = s.From("|")?.To(":");
                    string? content = s.From(":")?.Trim();

                    LlamaMessage message = new(displayName, content, LlamaTokenType.Input, this._tokenCache);
                    this._contextModel.Messages.Add(message);
                }

                if (!string.IsNullOrWhiteSpace(this._characterConfiguration.Prompt))
                {
                    this._contextModel.Instruction = new LlamaTokenBlock(FileService.GetStringOrContent(this._characterConfiguration.Prompt), LlamaTokenType.Prompt, this._tokenCache);
                }
            }

            this._context = await this._contextModel.GetState();
            await this._client.Eval(this.Context, 0);
            this.LoopingThread = new Thread(async () => await this.LoopProcess());
            this.LoopingThread.Start();
        }

        private async Task LoopProcess()
        {
            do
            {
                await this.TrySummarize();

                this.WaitForNext();

                await this.PrepRemoteContext();

                await this.ReadNextResponse();

                await this.CleanLastResponse();

                this._contextModel.RemoveTemporary();

                await this.Cleanup();

                await this.Save();
            } while (true);
        }

        private async Task CleanLastResponse()
        {
            if (!this._contextModel.Messages.Any())
            {
                return;
            }

            if (this._contextModel.Messages.Peek() is not LlamaMessage lastMessage || await lastMessage.UserName.Value != this._characterConfiguration.CharacterName)
            {
                return;
            }

            string? content = await lastMessage.Content.Value;

            if (content is null)
            {
                return;
            }

            string cleanedContent = this._responseCleaners.Clean(content);

            if (cleanedContent != content)
            {
                LlamaTokenCollection newContent = await this._client.Tokenize(cleanedContent);

                LlamaMessage newMessage = new(lastMessage.UserName, newContent, lastMessage.Type, this._tokenCache);

                this._contextModel.Messages.Pop();
                this._contextModel.Messages.Push(newMessage);
            }
        }

        private async Task PrepRemoteContext()
        {
            LlamaTokenCollection contextState = await this._contextModel.GetState();

            contextState.Append(await this._tokenCache.Get($"|{this.CharacterName}:"));

            this._context = contextState;

            await this._client.Eval(contextState, 0);
        }

        private async Task ReadNextResponse()
        {
            IReadOnlyLlamaTokenCollection thisInference = await this.Infer();

            await this.ReceiveResponse(thisInference);
        }

        private async Task ReceiveResponse(IReadOnlyLlamaTokenCollection collection)
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
                SourceChannel = LastChannel,
                Type = LlamaTokenType.Response
            };

            LlamaMessage message = new(this.CharacterName, collection, LlamaTokenType.Response, this._tokenCache);

            if (chatEntry.Content != null)
            {
                message.Id = this._chatService.Save(chatEntry);
            }

            this._contextModel.Messages.Enqueue(message);

            _ = this.SetState(AiState.Idle);
        }

        private async Task SendText(ChatEntry chatEntry, bool flush)
        {
            await this.Initialization;

            _ = this.SetState(AiState.Processing);

            if (chatEntry.Type == LlamaTokenType.Undefined)
            {
                chatEntry.Type = LlamaTokenType.Input;
            }

            long thisMessageId = this._chatService.Save(chatEntry);

            this.LastMessageId = thisMessageId;

            this.LastChannel = chatEntry.SourceChannel;

            string toSend;

            if (!string.IsNullOrWhiteSpace(chatEntry.DisplayName))
            {
                this._contextModel.Messages.Enqueue(new LlamaMessage(chatEntry.DisplayName, chatEntry.Content, chatEntry.Type, this._tokenCache) { Id = thisMessageId});
            }
            else
            {
                this._contextModel.Messages.Enqueue(new LlamaTokenBlock(chatEntry.Content, chatEntry.Type, this._tokenCache) { Id = thisMessageId });
            }

            if (flush)
            {
                this.ReturnControl(true);
            }
        }

        private bool SetState(AiState state)
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

        private async Task TrySummarize()
        {
            int c = (await this._contextModel.ToCollection()).Count;
            bool gtHalf = c > this._characterConfiguration.ContextLength * .5;
            bool gtQuart = c > this._characterConfiguration.ContextLength * .75;

            if (this._summaryTask != null && gtQuart)
            {
                SummaryResponse summary = await this._summaryTask;

                IReadOnlyLlamaTokenCollection tokens = await this._tokenCache.Get(summary.Summary, false);

                this._contextModel.Summary = new LlamaTokenBlock(tokens, LlamaTokenType.Undefined);

                while (this._contextModel.Messages.First().Id <= summary.FirstId)
                {
                    this._contextModel.Messages.Dequeue();
                }

                this._summaryTask = null;

                c = (await this._contextModel.ToCollection()).Count;
                gtHalf = c > this._characterConfiguration.ContextLength * .5;
            }

            if (this._summaryTask is null && gtHalf)
            {
                int tokenCount = 0;
                long lastMessageId = 0;
                int i = 0;

                while (tokenCount < this._characterConfiguration.ContextLength / 4)
                {
                    ITokenCollection block = this._contextModel.Messages[i];

                    if (block.Type != LlamaTokenType.Temporary)
                    {
                        tokenCount += await block.Count();
                        lastMessageId = Math.Max(block.Id, lastMessageId);
                        tokenCount++;
                    }

                    i++;
                }

                string summary = null;

                if (this._contextModel.Summary != null)
                {
                    summary = (await this._contextModel.Summary.ToCollection()).ToString().Trim('[').Trim(']').Trim();
                }

                this._summaryTask = this._summarizationService.Summarize(summary, lastMessageId, this.GetMessagesReversed(lastMessageId));
            }
        }

        public IEnumerable<string> GetMessagesReversed(long before)
        {
            do
            {
                ChatEntry ce = this._chatService.GetBefore(before);

                if (ce is null)
                {
                    yield break;
                }

                if (ce.Type == LlamaTokenType.Temporary)
                {
                    continue;
                }

                string c = StripNonASCII(ce.Content);

                if (string.IsNullOrWhiteSpace(ce.UserId))
                {
                    yield return $"[{c}]";
                }
                else
                {
                    string n = ce.DisplayName;

                    if (string.IsNullOrWhiteSpace(n))
                    {
                        n = ce.UserId;
                    }

                    yield return $"{n}: {c}";
                }

                before = ce.Id;
            } while (true);
        }

        public static string StripNonASCII(string input)
        {
            StringBuilder sb = new();
            foreach (char c in input)
            {
                if (c is >= (char)0 and <= (char)127) // ASCII range
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private void Unlock() => _ = this._chatLock.Release();

        private void WaitForNext()
        {
            this._acceptingInput.Set();

            this._processingInput.WaitOne();
        }
    }
}