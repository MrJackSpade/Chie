using Llama.Data.Scheduler;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class PredictRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("logitBias")]
        public Dictionary<int, float> LogitBias { get; set; } = new Dictionary<int, float>();

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }
    }
}
