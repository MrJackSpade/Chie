using System.Text.Json.Serialization;

namespace Summary.Models
{
    public class SummaryResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}