using LlamaApi.Models;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class JobResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("result")]
        public JsonNode? Result { get; set; }

        [JsonPropertyName("state")]
        public JobState State { get; set; }
    }

    public class JobResponse<TResult>
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("result")]
        public TResult? Result { get; set; }

        [JsonPropertyName("state")]
        public JobState State { get; set; }
    }
}