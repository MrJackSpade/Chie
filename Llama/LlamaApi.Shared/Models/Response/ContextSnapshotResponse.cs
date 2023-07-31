using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class ContextSnapshotResponse
    {
        [JsonPropertyName("tokens")]
        public ResponseLlamaToken[] Tokens { get; set; } = Array.Empty<ResponseLlamaToken>();
    }
}