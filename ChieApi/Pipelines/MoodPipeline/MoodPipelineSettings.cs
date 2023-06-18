using System.Text.Json.Serialization;

namespace ChieApi.Pipelines.MoodPipeline
{
    public class MoodPipelineSettings
    {
        [JsonPropertyName("cadenceQueueSize")]
        public int CadenceQueueSize { get; set; } = 3;

        [JsonPropertyName("events")]
        public MoodPipelineEvent[] Events { get; set; } = Array.Empty<MoodPipelineEvent>();

        [JsonPropertyName("firstMessage")]
        public bool FirstMessage { get; set; }

        [JsonPropertyName("minCadenceSeconds")]
        public int MinCadenceSeconds { get; set; } = 5000;

        [JsonPropertyName("minDelayMinutes")]
        public int MinDelayMinutes { get; set; }
    }
}