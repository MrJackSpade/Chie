using System.Text.Json.Serialization;

namespace Embedding.Models
{
    public class TokenizeResponse
    {
        [JsonPropertyName("exception")]
        public string Exception { get; set; }

        [JsonPropertyName("content")]
        public float[][] Content { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}