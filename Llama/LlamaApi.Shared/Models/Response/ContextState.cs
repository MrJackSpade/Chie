using System.Text.Json.Serialization;

namespace LlamaApi.Models.Response
{
    public class ContextState
    {
        [JsonPropertyName("availableBuffer")]
        public int AvailableBuffer { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("isLoaded")]
        public bool IsLoaded { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }
}