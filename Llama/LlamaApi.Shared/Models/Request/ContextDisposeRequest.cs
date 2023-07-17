using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Request
{
    public class ContextDisposeRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }
    }
}