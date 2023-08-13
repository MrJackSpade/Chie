using System.Text.Json.Serialization;

namespace Summary.Models
{
    public class TokenizeResponse
    {
        [JsonPropertyName("content")]
        public string[] Content { get; set; }

        [JsonPropertyName("exception")]
        public string Exception { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}