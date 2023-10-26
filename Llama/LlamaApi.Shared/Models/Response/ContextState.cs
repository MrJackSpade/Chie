using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class ContextState
    {
        [JsonPropertyName("availableBuffer")]
        public uint AvailableBuffer { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("isLoaded")]
        public bool IsLoaded { get; set; }

        [JsonPropertyName("size")]
        public uint Size { get; set; }
    }
}