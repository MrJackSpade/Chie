using System.Text.Json.Serialization;

namespace Summary.Models
{
    public class SummaryRequest
    {
        [JsonPropertyName("textData")]
        public string TextData { get; set; }

        [JsonPropertyName("maxLength")]
        public int MaxLength { get; set; }
    }
}