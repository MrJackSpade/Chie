using Llama.Data.Models;
using Llama.Data.Scheduler;

namespace LlamaApi.Models.Request
{
    public class PredictRequest
    {
        public Guid ContextId { get; set; }

        public LogitRuleCollection LogitRules { get; set; } = new();

        public bool NewPrediction { get; set; }

        public ExecutionPriority Priority { get; set; }
    }
}