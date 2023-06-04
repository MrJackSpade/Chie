using System.Text.Json.Serialization;

namespace ChieApi.Pipelines.MoodPipeline
{
	public class MoodPipelineEvent
	{
		[JsonPropertyName("chance")]
		public float Chance { get; set; }

		[JsonPropertyName("text")] 
		public string Text { get; set; }
	}
}
