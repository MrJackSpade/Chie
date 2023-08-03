using System.Text.Json.Serialization;

namespace Summary.Models
{
    public class TokenizeRequest
    {
        [JsonPropertyName("textData")]
        public string TextData { get; set; }
    }
}