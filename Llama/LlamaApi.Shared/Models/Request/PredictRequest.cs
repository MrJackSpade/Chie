using Llama.Data.Models;
using Llama.Data.Scheduler;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class PredictRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("logitBias")]
        public LogitRuleCollection LogitRules { get; set; } = new();

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }

        [JsonPropertyName("newPrediction")]
        public bool NewPrediction { get; set; }
    }
}