using System.Text.Json.Serialization;

namespace Embedding.Models
{
    public class TokenizeRequest
    {
        [JsonPropertyName("textData")]
        public string[] TextData { get; set; }
    }
}