using Llama.Data.Enums;
using Llama.Data.Models;

namespace Llama.Data
{
    public record LlamaContextSettings
    {
        public uint EvalThreadCount { get; set; } = (uint)(Environment.ProcessorCount / 2);

        public uint BatchSize { get; set; } = 512;

        public uint ContextSize { get; set; }

        public LogitRuleCollection LogitRules { get; set; } = new();

        public string LoraAdapter { get; set; } = string.Empty;

        public string LoraBase { get; set; } = string.Empty;

        public MemoryMode MemoryMode { get; set; } = MemoryMode.Float16;

        public bool Perplexity { get; set; }

        public float RopeFrequencyBase { get; set; } = 10_000;

        public float RopeFrequencyScaling { get; set; } = 1.0f;

        public uint Seed { get; set; } = (uint)new Random().Next();

        public uint ThreadCount { get; set; } = (uint)Math.Max(Environment.ProcessorCount / 2, 1);

        public bool GenerateEmbedding { get; set; }

        public LlamaRopeScalingType RopeScalingType { get; set; } = LlamaRopeScalingType.Unspecified;

        public float YarnExtFactor { get; set; } = -1.0f;

        public float YarnAttnFactor { get; set; } = 1.0f;

        public float YarnBetaFast { get; set; } = 32.0f;

        public float YarnBetaSlow { get; set; } = 1.0f;

        public uint YarnOrigCtx { get; set; } = 0;
    }
}