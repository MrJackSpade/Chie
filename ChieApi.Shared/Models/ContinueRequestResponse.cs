using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
    public class ContinueRequestResponse
    {
        [JsonPropertyName("success")]
        public bool Success => this.MessageId != 0;

        [JsonPropertyName("messageId")]
        public long MessageId { get; set; }
    }
}