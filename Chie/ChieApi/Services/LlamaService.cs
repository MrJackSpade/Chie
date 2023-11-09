using Ai.Abstractions;
using Ai.Utils.Extensions;
using ChieApi.CleanupPipeline;
using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Samplers;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using ChieApi.TokenTransformers;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using LlamaApi.Shared.Extensions;
using LlamaApi.Shared.Interfaces;
using LlamaApiClient;
using Loxifi;
using Loxifi.AsyncExtensions;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
    public partial class LlamaService
    {
        public const int SUMMARY_TARGET = 2000;

        private const int SUMMARY_CHUNKS = 12;

        private readonly AutoResetEvent _acceptingInput = new(false);

        private readonly CharacterConfiguration _characterConfiguration;

        private readonly SemaphoreSlim _chatLock = new(1);

        private readonly ChatRepository _chatService;

        private readonly LlamaContextClient _client;

        private readonly List<ITextCleaner> _inputCleaner;

        private readonly ILogger _logger;

        private readonly LogitService _logitService;

        private readonly List<IPostAccept> _postAccept;

        private readonly AutoResetEventWithData<ProcessInputData> _processingInput = new(false);

        private readonly List<ITextCleaner> _responseCleaners;

        private readonly List<ISimpleSampler> _simpleSamplers;

        private readonly SummarizationService _summarizationService;

        private readonly LlamaTokenCache _tokenCache;

        private readonly List<ITokenTransformer> _transformers;

        private readonly UserDataRepository _userDataService;

        private LlamaTokenCollection _context;

        private LlamaContextModel _contextModel;

        private Task<SummaryResponse>? _summaryTask;

        public LlamaService(UserDataRepository userDataService, SummarizationService summarizationService, DictionaryRepository dictionaryService, CharacterConfiguration characterConfiguration, LlamaContextClient client, LlamaContextModel contextModel, LlamaTokenCache llamaTokenCache, ILogger logger, ChatRepository chatService, LogitService logitService)
        {
            _userDataService = userDataService;
            _summarizationService = summarizationService;
            _client = client;
            _logitService = logitService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _contextModel = contextModel;
            _tokenCache = llamaTokenCache;
            _characterConfiguration = characterConfiguration;

            logger.LogInformation("Constructing Llama Service");

            _ = SetState(AiState.Initializing);

            _simpleSamplers = new List<ISimpleSampler>()
            {
                new NewlineEnsureSampler(llamaTokenCache),
                new RepetitionBlockingSampler(3)
            };

            TextTruncationTransformer textTruncation = new(1000, 500, 150, 0, ".!?…*", dictionaryService);

            _transformers = new List<ITokenTransformer>()
            {
                new SpaceStartTransformer(),
                new NewlineTransformer(),
                textTruncation,
                //new TextExtensionTransformer(100, 150),
                new RepetitionBlockingTransformer(3),
                new InvalidCharacterBlockingTransformer()
            };

            _inputCleaner = new List<ITextCleaner>()
            {
                new AsteriskSpacingCleaner()
            };

            _responseCleaners = new List<ITextCleaner>()
            {
                new InvalidContractionsCleaner(),
                new SpellingCleaner(dictionaryService),
                new DanglingQuoteCleaner(),
                new PunctuationCleaner(),
                new UnbrokenWordsCleaner(dictionaryService, 2),
                new BrokenWordsCleaner(dictionaryService, 3),
                new SentenceLevelPunctuationCleaner(),
                new DuplicateSentenceRemover(),
                new AsteriskSpacingCleaner(),
                textTruncation
            };

            _postAccept = new List<IPostAccept>()
            {
                new AsteriskAlignmentTransformer(_characterConfiguration.AsteriskCap),
                new CumulativeInferrenceRepetitionBias(1.5f, 3),
                new TabNewlineOnly()
            };

            Initialization = Task.Run(Init);
        }

        public AiState AiState { get; private set; }

        public string CharacterName { get; private set; }

        public IReadOnlyLlamaTokenCollection Context => _context;

        public string CurrentResponse { get; private set; }

        public Task Initialization { get; private set; }

        public Thread LoopingThread { get; private set; }

        private string LastChannel { get; set; }

        private long LastMessageId { get; set; }

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

        public LlamaClientResponseState CheckIfResponding(string channel)
        {
            LlamaClientResponseState responseState = new()
            {
                IsTyping = LastChannel == channel && AiState == AiState.Responding
            };

            if (responseState.IsTyping)
            {
                responseState.Content = CurrentResponse;
            }

            return responseState;
        }

        public IEnumerable<string> GetMessagesReversed(long before)
        {
            do
            {
                ChatEntry ce = _chatService.GetBefore(before);

                if (ce.DateCreated < _characterConfiguration.MemoryStart)
                {
                    yield break;
                }

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

        public ChatEntry[] GetResponses(string channelId, long after)
        {
            return _chatService.GetMessages(channelId, after, CharacterName);
        }

        public long ReturnControl(bool force, bool continuation = false, string? channelId = null)
        {
            bool channelAllowed = channelId == null || LastChannel == channelId;
            bool clearToProceed = channelAllowed && (force || _acceptingInput.WaitOne(100));

            long lastMessageId = LastMessageId;

            if (!clearToProceed)
            {
                _acceptingInput.Set();
            }
            else
            {
                _processingInput.SetDataAndSet(new ProcessInputData() { Continuation = continuation });
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
            await contextSaveState.LoadFrom(_contextModel);
            contextSaveState.SaveTo(saveInfo.FullName);
        }

        public async Task<long> Send(ChatEntry chatEntry)
        {
            return await Send(new ChatEntry[] { chatEntry });
        }

        public async Task<long> Send(ChatEntry[] chatEntries)
        {
            await Initialization;

            try
            {
                if (!TryLock())
                {
                    _logger.LogWarning($"Client not idle. Skipping ({chatEntries.Length}) messages.");
                    return 0;
                }

                _logger.LogInformation("Sending messages to client...");

                foreach (ChatEntry chat in chatEntries)
                {
                    chat.Content = _inputCleaner.Clean(chat.Content).Trim();
                }

                LlamaSafeString[] cleanedMessages = chatEntries.Select(LlamaSafeString.Parse)
                                                               .ToArray();

                if (cleanedMessages.Length != 0)
                {
                    for (int i = 0; i < chatEntries.Length; i++)
                    {
                        bool last = i == chatEntries.Length - 1;

                        LlamaSafeString cleanedMessage = cleanedMessages[i];

                        await SendText(chatEntries[i], last);
                    }
                }

                _logger.LogInformation($"Last Message Id: {LastMessageId}");

                return LastMessageId;
            }
            finally
            {
                Unlock();
            }
        }

        public bool TryGetReply(long originalMessageId, out ChatEntry? chatEntry)
        {
            return _chatService.TryGetOriginal(originalMessageId, out chatEntry);
        }

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
                    _contextModel = contextSaveState.ToModel(_tokenCache);
                    return true;
                }
            }

            return false;
        }

        private async Task CleanLastResponse()
        {
            TryGetLastBotMessage r = await TryGetLastMessageIsBot();

            if (!r.Success)
            {
                return;
            }

            string? content = await r.Message.Content.Value;

            if (content is null)
            {
                return;
            }

            string cleanedContent = " " + _responseCleaners.Clean(content).Trim();

            if (cleanedContent != content)
            {
                IReadOnlyLlamaTokenCollection newContent = await _client.Tokenize(cleanedContent);

                LlamaMessage newMessage = new(r.Message.UserName, newContent, r.Message.Type, _tokenCache);

                _contextModel.Messages.Pop();
                _contextModel.Messages.Push(newMessage);
            }
        }

        private async Task Cleanup()
        {
            LlamaTokenCollection contextState = await _contextModel.GetState();

            _context = contextState;

            if (contextState.Count > _characterConfiguration.ContextLength)
            {
                Debugger.Break();
                throw new Exception("Context length too long?");
            }

            await _client.Eval(contextState, 0);

            CurrentResponse = string.Empty;
        }

        private async Task<IReadOnlyLlamaTokenCollection> Infer()
        {
            InferenceEnumerator enumerator = _client.Infer();

            enumerator.SetBias(LlamaToken.EOS.Id, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            enumerator.SetBias(LlamaToken.NewLine.Id, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            enumerator.SetBias(_characterConfiguration.LogitBias, LogitRuleLifetime.Inferrence, LogitBiasType.Multiplicative);

            while (await enumerator.MoveNextAsync())
            {
                SetState(AiState.Responding);

                LlamaToken selected = new(enumerator.Current.Id, enumerator.Current.Value);

                Debug.WriteLine($"Predict: {enumerator.Current.Id} ({enumerator.Current.Value})");

                await foreach (LlamaToken llamaToken in _transformers.Transform(enumerator, selected))
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

                    foreach (IPostAccept postAccept in _postAccept)
                    {
                        postAccept.PostAccept(enumerator);
                    }

                    CurrentResponse += llamaToken.Value;

                    _context.Append(llamaToken);
                }

                if (!enumerator.Accepted)
                {
                    enumerator.MoveBack();
                }

                await _simpleSamplers.SampleNext(enumerator);
            }

            return enumerator.Enumerated.TrimWhiteSpace();
        }

        private async Task Init()
        {
            _logger.LogInformation("Constructing Llama Client");

            CharacterName = _characterConfiguration.CharacterName;

            _logger.LogDebug(System.Text.Json.JsonSerializer.Serialize(_characterConfiguration, new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            }));

            if (AiState == AiState.Initializing)
            {
                _ = SetState(AiState.Idle);
            }

            if (!TryLoad())
            {
                Console.WriteLine("No state found... Prompting...");

                foreach (string s in FileService.GetStringOrContent(_characterConfiguration.Start).CleanSplit())
                {
                    string? displayName = s.From("|")?.To(":");
                    string? content = s.From(":")?.Trim();

                    LlamaMessage message = new(displayName, content, LlamaTokenType.Input, _tokenCache);
                    _contextModel.Messages.Add(message);
                }

                if (!string.IsNullOrWhiteSpace(_characterConfiguration.InstructionBlock))
                {
                    string prompt = FileService.GetStringOrContent(_characterConfiguration.InstructionBlock);
                    prompt = prompt.Replace("\r", "");
                    _contextModel.InstructionBlock = new LlamaTokenBlock(prompt, LlamaTokenType.Prompt, _tokenCache);
                }

                if (!string.IsNullOrWhiteSpace(_characterConfiguration.AssistantBlock))
                {
                    string prompt = FileService.GetStringOrContent(_characterConfiguration.AssistantBlock);
                    prompt = prompt.Replace("\r", "");
                    _contextModel.AssistantBlock = new LlamaTokenBlock(prompt, LlamaTokenType.Prompt, _tokenCache);
                }
            }
            else
            {
                await ValidateUserSummaries();
            }

            _context = await _contextModel.GetState();
            await _client.Eval(Context, 0);
            LoopingThread = new Thread(async () => await LoopProcess());
            LoopingThread.Start();
        }

        private async Task LoopProcess()
        {
            do
            {
                await TrySummarize();

                ProcessInputData processingData = WaitForNext();
                TryGetLastBotMessage r = await TryGetLastMessageIsBot();
                bool continuation = processingData.Continuation && r.Success;

                await ValidateUserSummaries();

                await PrepRemoteContext(continuation);

                await ReadNextResponse(r);

                await CleanLastResponse();

                _contextModel.RemoveTemporary();

                await Cleanup();

                await Save();
            } while (true);
        }

        private async Task PrepRemoteContext(bool continuation)
        {
            LlamaTokenCollection contextState = await _contextModel.GetState();

            if (!continuation)
            {
                contextState.Append(await _tokenCache.Get($"\n|{CharacterName}:"));
            }

            _context = contextState;

            await _client.Eval(contextState, 0);
        }

        private async Task ReadNextResponse(TryGetLastBotMessage shouldAppend)
        {
            IReadOnlyLlamaTokenCollection thisInference = await Infer();

            long existingId = 0;

            if (shouldAppend.Success)
            {
                LlamaTokenCollection collection = new();
                await collection.Append(shouldAppend.Message.Content);
                collection.Append(thisInference);
                thisInference = collection;
                _contextModel.Messages.Remove(shouldAppend.Message);
                existingId = shouldAppend.Message.Id;
            }

            await ReceiveResponse(thisInference, existingId);
        }

        private async Task ReceiveResponse(IReadOnlyLlamaTokenCollection collection, long existingId)
        {
            foreach (LlamaToken token in collection)
            {
                _logitService.Identify(token.Id, token.Value, token.GetEscapedValue());
            }

            string responseContent = collection.ToString();

            string? userName = CharacterName;
            string? content = responseContent.To("|")!.Trim();
            CurrentResponse = string.Empty;

            if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Empty message returned. Ignoring...");
                return;
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = CharacterName;
            }

            ChatEntry chatEntry = new()
            {
                ReplyToId = LastMessageId,
                Content = content,
                DisplayName = userName,
                SourceChannel = LastChannel,
                Type = LlamaTokenType.Response,
                Id = existingId
            };

            LlamaMessage message = new(CharacterName, collection, LlamaTokenType.Response, _tokenCache);

            if (chatEntry.Content != null)
            {
                message.Id = _chatService.Save(chatEntry);
            }

            _contextModel.Messages.Enqueue(message);

            _ = SetState(AiState.Idle);
        }

        private async Task SendText(ChatEntry chatEntry, bool flush)
        {
            await Initialization;

            _ = SetState(AiState.Processing);

            if (chatEntry.Type == LlamaTokenType.Undefined)
            {
                chatEntry.Type = LlamaTokenType.Input;
            }

            long thisMessageId = _chatService.Save(chatEntry);

            LastMessageId = thisMessageId;

            LastChannel = chatEntry.SourceChannel;

            string toSend;

            if (!string.IsNullOrWhiteSpace(chatEntry.DisplayName))
            {
                _contextModel.Messages.Enqueue(new LlamaMessage(chatEntry.DisplayName, chatEntry.Content, chatEntry.Type, _tokenCache) { Id = thisMessageId });
            }
            else
            {
                _contextModel.Messages.Enqueue(new LlamaTokenBlock(chatEntry.Content, chatEntry.Type, _tokenCache) { Id = thisMessageId });
            }

            if (flush)
            {
                ReturnControl(true);
            }
        }

        private bool SetState(AiState state)
        {
            if (AiState != state)
            {
                _logger.LogInformation("Setting client state: " + state.ToString());
                AiState = state;
                return true;
            }

            return false;
        }

        private async Task<TryGetLastBotMessage> TryGetLastMessageIsBot()
        {
            TryGetLastBotMessage toReturn = new();

            if (!_contextModel.Messages.Any() || _contextModel.Messages.Peek() is not LlamaMessage lastMessage || await lastMessage.UserName.Value != _characterConfiguration.CharacterName)
            {
                toReturn.Success = false;
            }
            else
            {
                toReturn.Success = true;
                toReturn.Message = lastMessage;
            }

            return toReturn;
        }

        private bool TryLock()
        {
            _chatLock.Wait();

            if (AiState != AiState.Idle)
            {
                return false;
            }

            return true;
        }

        private async Task TrySummarize()
        {
            List<ITokenCollection> messages = _contextModel.Messages;

            uint contextLen = _characterConfiguration.ContextLength;
            uint contextGen = (await _contextModel.ToCollection()).Count;
            bool existingSummary = (await _contextModel.Summary.ToCollection()).Count > 0;
            bool canSummarize = _characterConfiguration.MemoryStart < DateTime.Now;

            float sumNextStart = 0.6f;
            float sumNextLoad = 1 - (1 / (float)(SUMMARY_CHUNKS - 1));

            //We only want to gen/load when it makes sense, which is when we have a lot
            //of context or when theres no existing history
            bool shouldSumStart = contextGen > contextLen * sumNextStart || (!existingSummary && canSummarize);
            bool shouldSumLoad = contextGen > contextLen * sumNextLoad || (!existingSummary && _summaryTask != null && _summaryTask.IsCompleted && canSummarize);

            if (_summaryTask?.IsCanceled ?? false)
            {
                _summaryTask = null;
            }

            if (_summaryTask != null && shouldSumLoad)
            {
                await _summaryTask;

                SummaryResponse summary = await _summaryTask;
                string strSummary = summary.Summary.Replace("\r", "\n").Replace("\n\n", "\n");
                IReadOnlyLlamaTokenCollection tokens = await _tokenCache.Get(strSummary, false);

                _contextModel.Summary = new LlamaTokenBlock(tokens, LlamaTokenType.Undefined);

                while (messages[0].Id < summary.FirstId)
                {
                    messages.Dequeue();
                }

                _summaryTask = null;

                contextGen = (await _contextModel.ToCollection()).Count;
                shouldSumStart = contextGen > contextLen * sumNextStart;
            }

            if (_summaryTask is null && shouldSumStart)
            {
                long lastMessageId = 0;

                //Lets figure out what we should be targeting for removal
                //start with the context length
                uint targetLength = contextLen;

                //We need room for our summary
                targetLength -= SUMMARY_TARGET;

                //figure out how big a single chunk is
                //and then remove that. Thats our next block padding
                uint chunkLen = contextLen / SUMMARY_CHUNKS;
                targetLength -= chunkLen;

                //Now we figure out how many tokens we need to
                //remove from the current gen, to get in under the target
                int toRemove = (int)(contextGen - targetLength);

                //Figure out how many messages we have
                int numMessages = messages.Count;

                //Work up the message stack until we know how many messages we need to remove
                //to fit in under the limit we calculated
                for (int i = 0; i < numMessages && toRemove > 0; i++)
                {
                    if (messages[i].Type != LlamaTokenType.Temporary)
                    {
                        toRemove -= await messages[i].Count();

                        //Out summarization should start (working back)
                        //from before the first message we're going to remove.
                        //Its in the context now but it wont be once we load
                        //up the summary since we're going to purge it
                        lastMessageId = Math.Max(messages[i].Id, lastMessageId);
                        toRemove--;
                    }
                }

                if (lastMessageId == 0)
                {
                    List<long> possibleTargets = messages.Select(m => m.Id).Where(x => x != 0).ToList();

                    if (possibleTargets.Count > 0)
                    {
                        lastMessageId = possibleTargets.Min();
                    }
                }

                if (lastMessageId == 0)
                {
                    //Should be first gen only, this assumes we dont need to remove anything
                    //In that case we want to start summarizing at the end of the DB table
                    //which is just lastmessage + 1 for all intent and purpose and should
                    //end up removing nothing from the stack once we're done.
                    lastMessageId = _chatService.GetLastMessage().Id + 1;
                }

                _summaryTask = _summarizationService.Summarize(lastMessageId, SUMMARY_TARGET, GetMessagesReversed(lastMessageId));
            }
        }

        private void Unlock()
        {
            _ = _chatLock.Release();
        }

        private async Task ValidateUserSummaries()
        {
            List<string> activeUsers = await _contextModel.GetActiveUsers();

            List<string> existingSummaries = _contextModel.UserSummaries.Keys.ToList();

            foreach (string user in existingSummaries)
            {
                if (!activeUsers.Contains(user))
                {
                    _contextModel.UserSummaries.Remove(user);
                }

                activeUsers.Remove(user);
            }

            foreach (string user in activeUsers)
            {
                UserData ud = _userDataService.GetByDisplayName(user);

                if (ud != null)
                {
                    _contextModel.UserSummaries.Add(user, new LlamaUserSummary(user, ud.UserSummary, _tokenCache));
                }
            }
        }

        private ProcessInputData WaitForNext()
        {
            _acceptingInput.Set();

            return _processingInput.WaitOneAndGet();
        }
    }
}