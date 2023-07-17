using System.Text.Json.Serialization;

namespace LlamaApi.Models.Response
{
    public class ContextSnapshotResponse
    {
        [JsonPropertyName("tokens")]
        public ResponseLlamaToken[] Tokens { get; set; } = Array.Empty<ResponseLlamaToken>();
    }
}