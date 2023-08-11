using Llama.Data.Models;

namespace Llama.Data
{
    public record LlamaContextSettings
    {
        public int EvalThreadCount { get; set; } = Environment.ProcessorCount / 2;

        public int BatchSize { get; set; } = 512;

        public int ContextSize { get; set; }

        public LogitRuleCollection LogitRules { get; set; } = new();
    }
}