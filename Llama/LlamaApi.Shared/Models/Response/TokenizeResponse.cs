using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class TokenizeResponse
    {
        [JsonPropertyName("tokens")]
        public ResponseLlamaToken[] Tokens { get; set; }
    }
}