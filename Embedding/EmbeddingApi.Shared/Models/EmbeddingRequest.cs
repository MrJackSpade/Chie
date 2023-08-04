using System.Text.Json.Serialization;

namespace Embedding.Models
{
    public class EmbeddingRequest
    {
        [JsonPropertyName("textData")]
        public string[] TextData { get; set; }
    }
}