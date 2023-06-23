using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
    public class ContinueRequestResponse
    {
        [JsonPropertyName("messageId")]
        public long MessageId { get; set; }

        [JsonPropertyName("success")]
        public bool Success => this.MessageId != 0;
    }
}