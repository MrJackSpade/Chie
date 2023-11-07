using Llama.Data;
using Llama.Data.Scheduler;
using LlamaApi.Shared.Models.Request;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class ContextRequest
    {
        [JsonPropertyName("contextRequestSettings")]
        public ContextRequestSettings? ContextRequestSettings { get; set; }

        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("modelId")]
        public Guid ModelId { get; set; }

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }

        [JsonPropertyName("settings")]
        public LlamaContextSettings Settings { get; set; } = new LlamaContextSettings();
    }
}