using System.Text.Json.Serialization;

namespace Summary.Models
{
    public class SummaryRequest
    {
        [JsonPropertyName("maxLength")]
        public int MaxLength { get; set; }

        [JsonPropertyName("textData")]
        public string TextData { get; set; }
    }
}