using Llama.Data;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class ModelRequest
    {
        [JsonPropertyName("modelId")]
        public Guid? ModelId { get; set; }

        [JsonPropertyName("settings")]
        public LlamaModelSettings Settings { get; set; }
    }
}