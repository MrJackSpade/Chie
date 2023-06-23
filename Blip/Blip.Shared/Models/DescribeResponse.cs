using System.Text.Json.Serialization;

namespace Blip.Models
{
    public class DescribeResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}