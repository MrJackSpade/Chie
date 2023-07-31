using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class ContextResponse
    {
        [JsonPropertyName("state")]
        public ContextState State { get; set; }
    }
}