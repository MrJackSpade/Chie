using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class ModelResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}