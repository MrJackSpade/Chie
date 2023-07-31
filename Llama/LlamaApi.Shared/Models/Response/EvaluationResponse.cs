using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class EvaluationResponse
    {
        [JsonPropertyName("availableBuffer")]
        public int AvailableBuffer { get; set; }

        [JsonPropertyName("evaluated")]
        public int Evaluated { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("isLoaded")]
        public bool IsLoaded { get; set; }
    }
}