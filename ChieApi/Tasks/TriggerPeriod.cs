using System.Text.Json.Serialization;

namespace ChieApi.Tasks
{
	public class TriggerPeriod
	{
		[JsonPropertyName("endMinutes")]
		public uint EndMinutes { get; set; } = uint.MaxValue;

		[JsonPropertyName("startMinutes")]
		public uint StartMinutes { get; set; } = uint.MinValue;
	}
}