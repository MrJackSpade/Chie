using System.Text.Json.Serialization;

namespace ChieApi.Shared.Entities
{
	public class LogEntry
	{
		[JsonPropertyName("content")]
		public string? Content { get; set; }

		[JsonPropertyName("dateCreated")]
		public DateTime DateCreated { get; set; }

		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("level")]
		public LogLevel Level { get; set; }
	}
}