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
        public const int MAX_RESPONSE = 500;

        public const int MIN_RESPONSE = 150;

        public const float RESPONSE_LENGTH_ADJ = 0.1f;

        public const int SUMMARY_TARGET = 0;

        public const float TRIM_FROM = 0.95f;

        public const float TRIM_TO = 0.85f;

        private readonly AutoResetEvent _acceptingInput = new(false);

        private readonly CharacterConfiguration _characterConfiguration;

        private readonly SemaphoreSlim _chatLock = new(1);

        private readonly ChatRepository _chatService;

        private readonly LlamaClient _client;

        private readonly List<ITextCleaner> _inputCleaner;

        private readonly ILogger _logger;

        private readonly LogitService _logitService;

        private readonly List<IPostAccept> _postAccept;

        private readonly SpecialTokens _specialTokens;

        private readonly AutoResetEventWithData<ProcessInputData> _processingInput = new(false);

        private readonly List<ITextCleaner> _responseCleaners;

        private readonly List<ISimpleSampler> _simpleSamplers;

        private readonly LlamaTokenCache _tokenCache;

        private readonly List<ITokenTransformer> _transformers;

        private readonly UserDataRepository _userDataService;

        private LlamaTokenCollection _context;

        private LlamaContextModel _contextModel;

        private float _responseLengthBias = 0f;

        private readonly ResponseLengthManager _responseLengthManager;

        public LlamaService(UserDataRepository userDataService, DictionaryRepository dictionaryService, CharacterConfiguration characterConfiguration, LlamaClient client, LlamaContextModel contextModel, LlamaTokenCache llamaTokenCache, ILogger logger, ChatRepository chatService, LogitService logitService)
        {
            _userDataService = userDataService;
            _client = client;
            _logitService = logitService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _contextModel = contextModel;
            _tokenCache = llamaTokenCache;
            _characterConfiguration = characterConfiguration;
            _specialTokens = characterConfiguration.SpecialTokens;

            logger.LogInformation("Constructing Llama Service");

            _ = this.SetState(AiState.Initializing);

            _simpleSamplers = new List<ISimpleSampler>()
            {
                new NewlineEnsureSampler(llamaTokenCache, _specialTokens),
                new RepetitionBlockingSampler(3)
            };

            _responseLengthManager = new(1000, MAX_RESPONSE, MIN_RESPONSE, 0, ".!?…*", dictionaryService, _specialTokens);

            _transformers = new List<ITokenTransformer>()
            {
                new SpaceStartTransformer(),
                new NewlineTransformer(_specialTokens),
                _responseLengthManager,
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
                _responseLengthManager
            };

            _postAccept = new List<IPostAccept>()
            {
                new AsteriskAlignmentTransformer(_characterConfiguration.AsteriskCap, _tokenCache),
                new CumulativeInferrenceRepetitionBias(1.5f, 3),
                new TabNewlineOnly()
            };

            Initialization = Task.Run(this.Init);
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
            return await this.Send(new ChatEntry[] { chatEntry });
        }

        public async Task<long> Send(ChatEntry[] chatEntries)
        {
            await Initialization;

            try
            {
                if (!this.TryLock())
                {
                    _logger.LogWarning($"Client not idle. Skipping ({chatEntries.Length}) messages.");
                    return 0;
                }

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

                        await this.SendText(chatEntries[i], last);
                    }
                }

                _logger.LogInformation($"Last Message Id: {LastMessageId}");

                return LastMessageId;
            }
            finally
            {
                this.Unlock();
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

        private void AdjustResponseBias(IReadOnlyLlamaTokenCollection enumerated)
        {
            IReadOnlyLlamaTokenCollection response = enumerated.TrimWhiteSpace();

            string responseStr = response.ToString();

            if (responseStr.Length < MIN_RESPONSE)
            {
                //Lower bias adj to increase length
                _responseLengthBias -= RESPONSE_LENGTH_ADJ;
            }
            else if (_responseLengthBias < 0)
            {
                //If we fall within the expected range, we should be training
                //the model so we gradually back off
                _responseLengthBias += RESPONSE_LENGTH_ADJ;
            }

            //Not currently accounting for values over zero (would cause truncation)
            _responseLengthBias = Math.Min(_responseLengthBias, 0f);
        }

        private async Task CleanLastResponse()
        {
            TryGetLastBotMessage r = await this.TryGetLastMessageIsBot();

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

            await _client.Write(contextState, 0);

            CurrentResponse = string.Empty;
        }

        private async Task<IReadOnlyLlamaTokenCollection> Infer()
        {
            InferenceEnumerator enumerator = _client.Infer();

            bool setBias = true;

            LlamaToken lastToken = null;

            for (int i = _contextModel.Messages.Count - 1; i >= 0; i--)
            {
                ITokenCollection message = _contextModel.Messages[i];

                if (message is LlamaMessage lm && (await lm.UserName.Value) == _characterConfiguration.CharacterName)
                {
                    lastToken = (await lm.Content.Tokens).First();
                    break;
                }
            }

            if (lastToken != null)
            {
                enumerator.SetBias(lastToken.Id, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }

            enumerator.SetBias(_specialTokens.EOS, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            enumerator.SetBias(_specialTokens.NewLine, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            enumerator.SetBias(_characterConfiguration.LogitBias, LogitRuleLifetime.Inferrence, LogitBiasType.Multiplicative);

            while (await enumerator.MoveNextAsync())
            {
                this.SetState(AiState.Responding);

                LlamaToken selected = new(enumerator.Current.Id, enumerator.Current.Value);

                Console.WriteLine($"Predict: {enumerator.Current.Id} ({enumerator.Current.Value})");
                Debug.Write($"{enumerator.Current.Value}");

                await foreach (LlamaToken llamaToken in _transformers.Transform(enumerator, selected))
                {
                    //Neither of these need to be accepted because the local
                    //context manages both
                    if (llamaToken.Id == _specialTokens.EOS)
                    {
                        this.AdjustResponseBias(enumerator.Enumerated);
                        return enumerator.Enumerated;
                    }

                    if (llamaToken.Value != null)
                    {
                        Console.Write(llamaToken.Value);
                    }

                    await enumerator.Accept(llamaToken);

                    if (setBias)
                    {
                        //After we've gotten the first token we set the lengthening bias for the inferrence session
                        enumerator.SetBias(_specialTokens.EOS, _responseLengthBias, LogitRuleLifetime.Inferrence, LogitBiasType.Additive);
                        enumerator.SetBias(_specialTokens.NewLine, _responseLengthBias, LogitRuleLifetime.Inferrence, LogitBiasType.Additive);
                        setBias = false;
                    }

                    foreach (IPostAccept postAccept in _postAccept)
                    {
                        await postAccept.PostAccept(enumerator);
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

            IReadOnlyLlamaTokenCollection response = enumerator.Enumerated.TrimWhiteSpace();

            this.AdjustResponseBias(response);

            return response;
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
                _ = this.SetState(AiState.Idle);
            }

            bool newData = !this.TryLoad();
            bool loadPrompt = newData || _characterConfiguration.ReloadPrompt;

            if (newData)
            {
                Console.WriteLine("No state found... Prompting...");

                foreach (string s in FileService.GetStringOrContent(_characterConfiguration.Start).CleanSplit())
                {
                    string? displayName = s.From("|")?.To(":");
                    string? content = s.From(":")?.Trim();

                    LlamaMessage message = new(displayName, content, LlamaTokenType.Input, _tokenCache);
                    _contextModel.Messages.Add(message);
                }
            }
            else
            {
                await this.ValidateUserSummaries();
            }

            if (loadPrompt)
            {
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

            _context = await _contextModel.GetState();

            await _client.Write(Context, 0);
            await _client.Eval();

            LoopingThread = new Thread(async () => await this.LoopProcess());
            LoopingThread.Start();
        }

        private async Task LoopProcess()
        {
            do
            {
                await this.TryTrim();

                ProcessInputData processingData = this.WaitForNext();
                TryGetLastBotMessage r = await this.TryGetLastMessageIsBot();
                bool continuation = processingData.Continuation && r.Success;

                await this.ValidateUserSummaries();

                await this.PrepRemoteContext(continuation);

                await this.ReadNextResponse(r);

                await this.CleanLastResponse();

                _contextModel.RemoveTemporary();

                await this.Cleanup();

                await this.Save();
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

            await _client.Write(contextState, 0);
            await _client.Eval();
        }

        private async Task ReadNextResponse(TryGetLastBotMessage shouldAppend)
        {
            IReadOnlyLlamaTokenCollection thisInference = await this.Infer();

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

            await this.ReceiveResponse(thisInference, existingId);
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

            _ = this.SetState(AiState.Idle);
        }

        private async Task SendText(ChatEntry chatEntry, bool flush)
        {
            await Initialization;

            _ = this.SetState(AiState.Processing);

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
                this.ReturnControl(true);
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

        private async Task TryTrim()
        {
            uint contextGen = (await _contextModel.ToCollection()).Count;
            uint contextLen = _characterConfiguration.ContextLength;

            uint trimStart = (uint)(contextLen * TRIM_FROM);

            if (contextGen < trimStart)
            {
                return;
            }

            List<ITokenCollection> messages = _contextModel.Messages;

            uint trimTarget = (uint)(contextLen * TRIM_TO);

            int dropped = 0;

            while (contextGen > trimTarget)
            {
                dropped++;
                messages.Dequeue();
                contextGen = (await _contextModel.ToCollection()).Count;
            }

            if (dropped > 0)
            {
                Debug.WriteLine($"Dropped {dropped} from history");
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