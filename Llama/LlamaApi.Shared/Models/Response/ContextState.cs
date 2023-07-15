using System.Text.Json.Serialization;

namespace LlamaApi.Models.Response
{
    public class ContextState
    {
        [JsonPropertyName("isLoaded")]
        public bool IsLoaded { get; set; }
        [JsonPropertyName("id")]

        public Guid Id { get; set; }
        [JsonPropertyName("size")]

        public int Size { get; set; }
        [JsonPropertyName("availableBuffer")]

        public int AvailableBuffer { get; set; }
    }
}
