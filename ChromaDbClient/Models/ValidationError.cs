using System.Text.Json.Serialization;

namespace ChromaDbClient.Models
{
	public class ValidationError
	{
		[JsonPropertyName("loca")]
		public string Location { get; set; }

		[JsonPropertyName("msg")]
		public string Message { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; }
	}
}