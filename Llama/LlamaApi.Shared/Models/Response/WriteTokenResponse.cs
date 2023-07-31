using LlamaApi.Shared.Models.Response;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Response
{
    public class WriteTokenResponse
    {
        [JsonPropertyName("state")]
        public ContextState State { get; set; }
    }
}