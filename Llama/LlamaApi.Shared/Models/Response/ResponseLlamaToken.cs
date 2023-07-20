using Llama.Data.Enums;
using Llama.Data.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Response
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

        public string EscapedValue { get; set; }

        public int Id { get; set; }

        public JsonObject? TokenData { get; set; }

        public string Value { get; set; }
    }
}