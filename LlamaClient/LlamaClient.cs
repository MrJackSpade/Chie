using Llama.Collections;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Interfaces;
using Llama.Context.Samplers.FrequencyAndPresence;
using Llama.Context.Samplers.Interfaces;
using Llama.Context.Samplers.Mirostat;
using Llama.Context.Samplers.Repetition;
using Llama.Context.Samplers.Temperature;
using Llama.Data;
using Llama.Events;
using Llama.Model;
using Llama.Pipeline.ContextRollers;
using Llama.Pipeline.Interfaces;
using Llama.Pipeline.PostResponseContextTransformers;
using Llama.Pipeline.TokenTransformers;
using Llama.Shared;
using Llama.TokenTransformers;
using Llama.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Llama
{
    public class LlamaClient
    {
        private static Thread? _inferenceThread;

        private readonly Encoding _encoding = System.Text.Encoding.UTF8;

        private readonly LlamaModel _model;

        private readonly StringBuilder _outBuilder;

        private readonly List<InputText> _queued = new();

        private readonly Shared.LlamaSettings _settings;

        private bool _killSent;

        private char _lastChar;

        public LlamaClient(Shared.LlamaSettings settings)
        {
            this._settings = settings;
            this._outBuilder = new StringBuilder();

            IServiceCollection serviceDescriptors = new ServiceCollection();

            serviceDescriptors.AddSingleton<ITokenTransformer, NewlineEnsureTransformer>();
            serviceDescriptors.AddTransient((s) =>
            {
                LlamaModelSettings modelSettings = s.GetRequiredService<LlamaModelSettings>();
                LlamaContextSettings contextSettings = s.GetRequiredService<LlamaContextSettings>();
                return Utils.InitContextFromParams(modelSettings, contextSettings);
            });

            serviceDescriptors.AddTransient<IContext, LlamaContextWrapper>();

            serviceDescriptors.AddTransient<LlamaModel>();
            this.RegisterTemp(serviceDescriptors, settings);
            this.RegisterMirostat(serviceDescriptors, settings);
            this.RegisterRepeat(serviceDescriptors, settings);
            this.RegisterFrequency(serviceDescriptors, settings);
            this.RegisterModelSettings(serviceDescriptors, settings);
            this.RegisterContextSettings(serviceDescriptors, settings);

            serviceDescriptors.AddSingleton<IPostResponseContextTransformer, RemoveTemporaryTokens>();
            serviceDescriptors.AddSingleton<IPostResponseContextTransformer, StripNullTokens>();
            serviceDescriptors.AddSingleton<IContextRoller, ChatContextRoller>();
            serviceDescriptors.AddSingleton<ITokenTransformer, InteractiveEosReplace>();
            serviceDescriptors.AddSingleton<ITokenTransformer, InvalidCharacterBlockingTransformer>();
            serviceDescriptors.AddSingleton<ITokenTransformer, LetterFrequencyTransformer>();

            this._model = serviceDescriptors.BuildServiceProvider().GetRequiredService<LlamaModel>();

            this._model.OnContextModification += this.LlamaContext_OnContextModification;
        }

        public event Action<ContextModificationEventArgs> OnContextModification;

        public event EventHandler<DisconnectEventArgs> OnDisconnect;

        public event EventHandler<string>? ResponseReceived;

        public event EventHandler<LlameClientTokenGeneratedEventArgs>? TokenGenerated;

        public bool Connected { get; private set; }

        public bool EndsWithReverse => this._lastChar == this._settings.PrimaryReversePrompt?.Last();

        public bool HasQueuedMessages => this._queued.Count > 0;

        public LlamaSettings Params { get; private set; }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes); // .NET 5 +
        }

        public async Task Connect()
        {
            if (this._model.IsNewSession)
            {
                if (!string.IsNullOrWhiteSpace(this._settings.Start))
                {
                    string toSend = this._settings.Start;

                    if (File.Exists(this._settings.Start))
                    {
                        toSend = File.ReadAllText(this._settings.Start);
                    }

                    if (!this._settings.Prompt.EndsWith("\n"))
                    {
                        toSend = "\n" + toSend;
                    }

                    this.Send(toSend, LlamaTokenTags.INPUT, false);
                }
            }
        }

        /// <summary>
        /// Kills the thread and flushes the current output
        /// </summary>
        /// <returns></returns>
        public void Kill()
        {
            throw new NotImplementedException();
            this._killSent = true;
            string response = this._outBuilder.ToString();
            _ = this._outBuilder.Clear();
            this.ResponseReceived?.Invoke(this, response);
        }

        public void Send(string toSend, string tag, bool flush = true, bool validateReversal = true)
        {
            //If the last message was a manual kill, then we're not going to have the
            //expected turnaround token, so we need to make sure we append it to the beginning
            //of the next message so that its available in the right place in the context
            if (validateReversal && this._killSent && this._lastChar != this._settings.PrimaryReversePrompt.Last() && !toSend.StartsWith(this._settings.PrimaryReversePrompt.Last()))
            {
                toSend = $"{this._settings.PrimaryReversePrompt}{toSend}";
            }

            if (!flush)
            {
                toSend += "\n";
            }

            this._queued.Add(new InputText(toSend, tag));

            if (flush)
            {
                this.StartInference(this._queued.ToArray());

                this._queued.Clear();
            }

            this._killSent = false;
        }

        private void LlamaContext_OnContextModification(ContextModificationEventArgs obj) => this.OnContextModification?.Invoke(obj);

        private void RegisterContextSettings(IServiceCollection serviceDescriptors, LlamaSettings settings)
        {
            LlamaContextSettings c = new()
            {
                Encoding = Encoding.UTF8
            };

            switch (settings.InteractiveMode)
            {
                case InteractiveMode.None:
                    break;

                case InteractiveMode.Interactive:
                    c.Interactive = true;
                    break;

                case InteractiveMode.InteractiveFirst:
                    c.Interactive = true;
                    c.InteractiveFirst = true;
                    break;

                default: throw new NotImplementedException();
            }

            if (settings.UseSessionData)
            {
                string modelPathHash = CreateMD5(settings.ModelPath);
                c.SessionPath = modelPathHash + ".session";
            }

            if (settings.NoPenalizeNewLine)
            {
                c.PenalizeNewlines = false;
            }

            c.Antiprompt = settings.AllReversePrompts.ToList();

            if (!string.IsNullOrEmpty(settings.InSuffix))
            {
                c.InputSuffix = settings.InSuffix;
            }

            if (settings.ContextLength.HasValue)
            {
                c.ContextSize = settings.ContextLength.Value;
            }

            if (settings.TokensToPredict.HasValue)
            {
                c.PredictCount = settings.TokensToPredict.Value;
            }

            if (settings.KeepPromptTokens.HasValue)
            {
                c.KeepContextTokenCount = settings.KeepPromptTokens.Value;
            }

            if (!string.IsNullOrWhiteSpace(settings.Prompt))
            {
                if (File.Exists(settings.Prompt))
                {
                    c.Prompt = File.ReadAllText(settings.Prompt);
                }
                else
                {
                    c.Prompt = settings.Prompt;
                }
            }

            foreach (KeyValuePair<int, string> bias in settings.LogitBias)
            {
                if (string.Equals(bias.Value, "-inf"))
                {
                    c.LogitBias!.Add(bias.Key, float.NegativeInfinity);
                }
                else if (string.Equals(bias.Value, "+inf"))
                {
                    c.LogitBias!.Add(bias.Key, float.PositiveInfinity);
                }
                else
                {
                    c.LogitBias!.Add(bias.Key, float.Parse(bias.Value));
                }
            }

            serviceDescriptors.AddSingleton(c);
        }

        private void RegisterFrequency(IServiceCollection serviceDescriptors, LlamaSettings settings)
        {
            FrequencyAndPresenceSamplerSettings frequencyAndPresenceSamplerSettings = new();

            if(settings.RepeatPenaltyWindow.HasValue)
            {
                frequencyAndPresenceSamplerSettings.RepeatTokenPenaltyWindow = settings.RepeatPenaltyWindow.Value;
            }

            serviceDescriptors.AddSingleton(frequencyAndPresenceSamplerSettings);

            serviceDescriptors.AddSingleton<ISimpleSampler, FrequencyAndPresenceSampler>();
        }

        private void RegisterMirostat(IServiceCollection serviceDescriptors, LlamaSettings settings)
        {
            if (settings.MiroStat != MiroStatMode.Disabled)
            {
                MirostatSamplerSettings mirostatSamplerSettings = new();

                if (settings.MiroStatEntropy.HasValue)
                {
                    mirostatSamplerSettings.Eta = settings.MiroStatEntropy.Value;
                }

                serviceDescriptors.AddSingleton<MirostatSamplerSettings>();

                if (settings.MiroStat == MiroStatMode.MiroStat)
                {
                    serviceDescriptors.AddSingleton<MirostatOneSampler>();
                    return;
                }

                if (settings.MiroStat == MiroStatMode.MiroStat2)
                {
                    serviceDescriptors.AddSingleton<MirostatTwoSampler>();
                    return;
                }

                throw new NotImplementedException();
            }
        }

        private void RegisterModelSettings(IServiceCollection serviceDescriptors, LlamaSettings settings)
        {
            LlamaModelSettings p = new();

            if (settings.Threads.HasValue)
            {
                p.ThreadCount = settings.Threads.Value;
            }

            if (settings.NoMemoryMap)
            {
                p.UseMemoryMap = false;
                p.UseMemoryLock = true;
            }

            switch (settings.MemoryMode)
            {
                case MemoryMode.Float16:
                    break;

                case MemoryMode.Float32:
                    p.MemoryFloat16 = false;
                    break;

                default: throw new NotImplementedException();
            }

            if (settings.GpuLayers.HasValue)
            {
                p.GpuLayerCount = settings.GpuLayers.Value;
            }

            p.Model = settings.ModelPath;

            serviceDescriptors.AddSingleton(p);
        }

        private void RegisterRepeat(IServiceCollection serviceDescriptors, LlamaSettings settings)
        {
            RepetitionSamplerSettings repetitionSamplerSettings = new();

            if (settings.RepeatPenalty.HasValue)
            {
                repetitionSamplerSettings.RepeatPenalty = settings.RepeatPenalty.Value;
            }

            if (settings.RepeatPenaltyWindow.HasValue)
            {
                repetitionSamplerSettings.RepeatTokenPenaltyWindow = settings.RepeatPenaltyWindow.Value;
            }

            serviceDescriptors.AddSingleton(repetitionSamplerSettings);

            serviceDescriptors.AddSingleton<ISimpleSampler, RepetitionSampler>();
        }

        private void RegisterTemp(IServiceCollection serviceDescriptors, LlamaSettings settings)
        {
            TemperatureSamplerSettings temperatureSamplerSettings = new();

            if (settings.Temp.HasValue)
            {
                temperatureSamplerSettings.Temperature = settings.Temp.Value;
            }

            if (settings.Top_P.HasValue)
            {
                temperatureSamplerSettings.TopP = settings.Top_P.Value;
            }

            serviceDescriptors.AddSingleton(temperatureSamplerSettings);

            serviceDescriptors.AddSingleton<IFinalSampler, TemperatureSampler>();
        }

        private void StartInference(params InputText[] data)
        {
            _inferenceThread = new Thread(() =>
            {
                LlamaTokenCollection result = new();

                LlamaToken? lastChunk = null;

                int lastChunkCount = 0;

                foreach (LlamaToken chunk in this._model.Call(data))
                {
                    if (string.IsNullOrEmpty(chunk.Value))
                    {
                        continue;
                    }

                    this.TokenGenerated?.Invoke(this, new LlameClientTokenGeneratedEventArgs()
                    {
                        Token = chunk
                    });

                    result.Append(chunk);
                    this._lastChar = chunk.Value[^1];

                    if (lastChunk != chunk)
                    {
                        lastChunkCount = 0;
                        lastChunk = chunk;
                    }
                    else
                    {
                        lastChunkCount++;
                    }

                    if (lastChunkCount >= 10)
                    {
                        break;
                    }
                }

                string resultStr = result.ToString();

                if (!resultStr.Contains('\n'))
                {
                    Console.Write('\n');
                }

                this.ResponseReceived?.Invoke(this, resultStr);
            });

            _inferenceThread.Start();
        }
    }
}