using Llama.Data.Scheduler;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class WriteTokenRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }

        [JsonPropertyName("startIndex")]
        public int StartIndex { get; set; } = -1;

        [JsonPropertyName("tokens")]
        public List<RequestLlamaToken> Tokens { get; set; } = new();

        [JsonPropertyName("writeTokenType")]
        public WriteTokenType WriteTokenType { get; set; } = WriteTokenType.Overwrite;
    }
}