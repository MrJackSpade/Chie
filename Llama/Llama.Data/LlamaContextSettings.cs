using Llama.Data.Enums;
using Llama.Data.Models;

namespace Llama.Data
{
    public record LlamaContextSettings
    {
        /// <summary>
        /// Default CPU Count - 2
        /// </summary>
        public uint EvalThreadCount { get; set; } = (uint)(Environment.ProcessorCount / 2);

        /// <summary>
        /// Default 512
        /// </summary>
        public uint BatchSize { get; set; } = 512;

        public bool OffloadKQV { get; set; } = true;

        /// <summary>
        /// Default 4096
        /// </summary>
        public uint ContextSize { get; set; } = 4096;

        public LogitRuleCollection LogitRules { get; set; } = new();

        public string LoraAdapter { get; set; } = string.Empty;

        public string LoraBase { get; set; } = string.Empty;

        public GgmlType TypeK { get; set; } = GgmlType.GGML_TYPE_F16;

        public bool Perplexity { get; set; }

        /// <summary>
        /// Default 10_000 (Model Set)
        /// </summary>
        public float RopeFrequencyBase { get; set; } = 0;

        /// <summary>
        /// Default 1.0
        /// </summary>
        public float RopeFrequencyScaling { get; set; } = 0;

        public uint Seed { get; set; } = (uint)new Random().Next();

        /// <summary>
        /// Default CPU Count - 2
        /// </summary>
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