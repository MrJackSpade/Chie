using Llama.Data.Scheduler;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class TokenizeRequest
    {
        [Required(AllowEmptyStrings = true)]
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }
    }
}