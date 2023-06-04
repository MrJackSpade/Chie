using System.Text.Json.Serialization;

namespace ChieApi.Pipelines.MoodPipeline
{
	public class MoodPipelineSettings
	{
		[JsonPropertyName("events")]
		public MoodPipelineEvent[] Events { get; set; } = Array.Empty<MoodPipelineEvent>();	

		[JsonPropertyName("firstMessage")]
		public bool FirstMessage { get; set; } = true;

		[JsonPropertyName("minDelayMinutes")]
		public int MinDelayMinutes { get; set; }

		[JsonPropertyName("minCadenceSeconds")]
		public int MinCadenceSeconds { get; set; } = 5000;

		[JsonPropertyName("cadenceQueueSize")]
		public int CadenceQueueSize { get; set; } = 3;
	}
}