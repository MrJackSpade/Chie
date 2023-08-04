using System.Text.Json.Serialization;

namespace Embedding.Models
{
    public class EmbeddingResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}