using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Request
{
    public class SamplerSetting
    {
        [JsonPropertyName("settings")]
        public JsonObject Settings { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}