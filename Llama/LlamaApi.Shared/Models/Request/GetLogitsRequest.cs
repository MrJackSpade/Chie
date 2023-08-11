using Llama.Data.Scheduler;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Request
{
    public class GetLogitsRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }
    }
}
