﻿using Llama.Data.Enums;
using Llama.Data.Models;
using LlamaApi.Shared.Models.Response;

namespace ChieApi.Services
{
    public class LlamaSettings
    {
        public string[] AdditionalReversePrompts { get; set; } = Array.Empty<string>();

        public IEnumerable<string> AllReversePrompts
        {
            get
            {
                if (this.PrimaryReversePrompt != null)
                {
                    yield return this.PrimaryReversePrompt;
                }

                foreach (string additionalReversePrompt in this.AdditionalReversePrompts)
                {
                    yield return additionalReversePrompt;
                }
            }
        }

        public string? AssistantBlock { get; set; }

        public uint BatchSize { get; set; } = 512;

        public uint ContextLength { get; set; } = 2048;

        public bool GenerateEmbedding { get; set; }

        public int GpuLayers { get; set; } = 0;

        public string? InstructionBlock { get; set; }

        public float LearningRate { get; set; }

        public Dictionary<int, string> LogitBias { get; set; } = new Dictionary<int, string>();

        public float MaxTarget { get; set; } = 1f;

        public float MinTarget { get; set; } = 0f;

        public float Scale { get; set; } = 1f;

        public MirostatType MiroStat { get; set; }

        public float MiroStatEntropy { get; set; } = 5;

        public string ModelPath { get; set; }

        public bool NoMemoryMap { get; set; }

        public bool NoPenalizeNewLine { get; set; }

        public string? PrimaryReversePrompt { get; set; }

        public float RepeatPenalty { get; set; } = 1.1f;

        public int RepeatPenaltyWindow { get; set; } = 64;

        public bool ReturnOnNewLine { get; set; }

        public float RopeBase { get; set; } = 10_000;

        public float RopeScale { get; set; } = 1.0f;

        public LlamaRopeScalingType RopeScalingType { get; set; } = LlamaRopeScalingType.Linear;

        public SpecialTokens SpecialTokens { get; set; } = new SpecialTokens();

        public string? Start { get; set; }

        public float Temperature { get; set; } = 0.80f;

        public uint? Threads { get; set; }

        public int Timeout { get; set; } = 600_000;

        public int TopK { get; set; }

        public float TopP { get; set; } = 0.95f;

        public GgmlType TypeK { get; set; } = GgmlType.GGML_TYPE_F16;

        public bool UseGqa { get; set; }

        public bool UseSessionData { get; set; }

        public bool VerbosePrompt { get; set; }

        public float YarnAttnFactor { get; set; } = 1.0f;

        public float YarnBetaFast { get; set; } = 32.0f;

        public float YarnBetaSlow { get; set; } = 1.0f;

        public float YarnExtFactor { get; set; } = -1.0f;

        public uint YarnOrigCtx { get; set; } = 0;
    }
}