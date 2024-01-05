using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LlamaApi.Shared.Models.Request
{
    public class SamplerSetting
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("settings")]
        public JsonObject Settings { get; set; }
    }
}
