using Llama.Data.Enums;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class RequestLlamaToken
    {
        [JsonPropertyName("tokenData")]
        public JsonObject? TokenData { get; set; }

        [JsonPropertyName("tokenId")]
        public int TokenId { get; set; }

        [JsonPropertyName("tokenType")]
        public LlamaTokenType TokenType { get; set; }
    }
}