using Llama.Data.Enums;
using Llama.Data.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class ResponseLlamaToken
    {
        [JsonConstructor]
        public ResponseLlamaToken()
        {
        }

        public ResponseLlamaToken(LlamaToken t)
        {
            this.EscapedValue = t.GetEscapedValue();
            this.Id = t.Id;
            this.Value = t.Value;

            if (t.Data != null)
            {
                if (t.Data is JsonObject jo)
                {
                    this.TokenData = jo;
                }
                else
                {
                    this.TokenData = (JsonObject?)JsonNode.Parse(JsonSerializer.Serialize(t.Data));
                }
            }
        }

        [JsonPropertyName("escapedValue")]
        public string EscapedValue { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tokenData")]
        public JsonObject? TokenData { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}