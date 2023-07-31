using Llama.Data.Models;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class TokenizeResponse
    {
        [JsonPropertyName("tokens")]
        public LlamaToken[] Tokens { get; set; }
    }
}