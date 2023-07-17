using Llama.Context;
using Llama.Context.Factories;
using Llama.Context.Interfaces;
using Llama.Context.Samplers.FrequencyAndPresence;
using Llama.Context.Samplers.Interfaces;
using Llama.Context.Samplers.Mirostat;
using Llama.Context.Samplers.Repetition;
using Llama.Context.Samplers.Temperature;
using Llama.Model;
using Llama.Model.Interfaces;
using Llama.Native;
using Llama.Pipeline;
using Llama.Pipeline.ContextRollers;
using Llama.Pipeline.Interfaces;
using Llama.Pipeline.PostResponseContextTransformers;
using Llama.Pipeline.Summarizers;
using Llama.Pipeline.TokenTransformers;
using Llama.Samplers;
using Llama.Scheduler;
using Llama.Shared;
using Llama.TokenTransformers;
using System.Text;

namespace Llama
{
    public class ContextEvaluatorBuilder
    {
        private readonly IExecutionScheduler _executionScheduler;

        private readonly IContextHandleFactory _llamaContextFactory;

        private readonly IModelHandleFactory _llamaModelFactory;

        private readonly List<IPostResponseContextTransformer> _postResponseTransforms = new();

        private readonly List<ISimpleSampler> _simpleSamplers = new();

        private readonly ITextSanitizer _textSanitizer;

        private readonly List<ITokenTransformer> _tokensTransformers = new();

        private LlamaContextSettings _contextSettings;

        private IFinalSampler _finalSampler;

        private LlamaModelSettings _modelSettings;

        public ContextEvaluatorBuilder(LlamaSettings settings)
        {
            this.ContextSettings(settings);
            this.ModelSettings(settings);
            this.FinalSampler(settings);
            this.Frequency(settings);
            this.Repeat(settings);
            this._simpleSamplers.Add(new NewlineEnsureSampler());
            this._textSanitizer = new ChatTextSanitizer();
            this._executionScheduler = new ExecutionScheduler();
            this._llamaModelFactory = new SingletonModelFactory(this._modelSettings, this._contextSettings);
            this._llamaContextFactory = new MultipleContextFactory(this._llamaModelFactory.Create(), this._modelSettings, this._contextSettings);

            this._tokensTransformers = new List<ITokenTransformer>()
            {
                new InteractiveEosReplace(),
                new InvalidCharacterBlockingTransformer()
            };

            this._postResponseTransforms = new List<IPostResponseContextTransformer>()
            {
                new RemoveTemporaryTokens(),
                new StripNullTokens()
              
            };
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes); // .NET 5 +
        }

        public LlamaContextWrapper BuildChatContextWrapper(IContextRoller roller, IEnumerable<ITokenTransformer> additionalTransformers = null)
        {
            List<ITokenTransformer> transformers = new();

            transformers.AddRange(this._tokensTransformers);

            if(additionalTransformers != null)
            {
                transformers.AddRange(additionalTransformers);
            }

            SafeLlamaContextHandle context = this._llamaContextFactory.Create();
            LlamaContextWrapper wrapper = new(this._executionScheduler, this._textSanitizer, context, this._modelSettings, this._contextSettings, this._postResponseTransforms, transformers, this._simpleSamplers, this._finalSampler);
            return wrapper;
        }

        public ContextEvaluator BuildChatEvaluator()
        {
            List<ITokenTransformer> transformers = new() { new TextTruncationTransformer(250, 150, ".!?") };

            transformers.AddRange(this._tokensTransformers);

            IBlockProcessor chatSummarizer = new ChatSummarizer(this._contextSettings);
            IContextRoller contextRoller = new ChatContextRoller(chatSummarizer);

            LlamaContextWrapper wrapper = this.BuildChatContextWrapper(contextRoller, transformers);

            ContextEvaluator chatEvaluator = new(wrapper, this._textSanitizer, contextRoller, this._contextSettings);
            chatEvaluator.QueueWritten += (s, e) => chatSummarizer.Process(e);

            if (!chatEvaluator.IsNewSession)
            {
                chatSummarizer.Process(wrapper.Buffer.Trim());
            }

            return chatEvaluator;
        }

        private void ContextSettings(LlamaSettings settings)
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

            this._contextSettings = c;
        }

        private void FinalSampler(LlamaSettings settings)
        {
            if (settings.MiroStat != MiroStatMode.Disabled)
            {
                MirostatSamplerSettings mirostatSamplerSettings = new();

                if (settings.MiroStatEntropy.HasValue)
                {
                    mirostatSamplerSettings.Eta = settings.MiroStatEntropy.Value;
                }

                if (settings.Temp.HasValue)
                {
                    mirostatSamplerSettings.Temperature = settings.Temp.Value;
                }

                if (settings.MiroStat == MiroStatMode.MiroStat)
                {
                    this._finalSampler = new MirostatOneSampler(mirostatSamplerSettings);
                    return;
                }

                if (settings.MiroStat == MiroStatMode.MiroStat2)
                {
                    this._finalSampler = new MirostatTwoSampler(mirostatSamplerSettings);
                    return;
                }

                throw new NotImplementedException();
            }
            else
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

                this._finalSampler = new TemperatureSampler(temperatureSamplerSettings);
            }
        }

        private void Frequency(LlamaSettings settings)
        {
            FrequencyAndPresenceSamplerSettings frequencyAndPresenceSamplerSettings = new();

            if (settings.RepeatPenaltyWindow.HasValue)
            {
                frequencyAndPresenceSamplerSettings.RepeatTokenPenaltyWindow = settings.RepeatPenaltyWindow.Value;
            }

            this._simpleSamplers.Add(new FrequencyAndPresenceSampler(frequencyAndPresenceSamplerSettings));
        }

        private void ModelSettings(LlamaSettings settings)
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

            p.GenerateEmbedding = settings.GenerateEmbedding;

            this._modelSettings = p;
        }

        private void Repeat(LlamaSettings settings)
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

            this._simpleSamplers.Add(new RepetitionSampler(repetitionSamplerSettings));
            this._simpleSamplers.Add(new RepetitionCapSampler(5));
        }
    }
}