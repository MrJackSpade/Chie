using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
    public class MessageSendResponse
    {
        [JsonPropertyName("messageId")]
        public long MessageId { get; set; }

        public bool Success => this.MessageId != 0;
    }
}