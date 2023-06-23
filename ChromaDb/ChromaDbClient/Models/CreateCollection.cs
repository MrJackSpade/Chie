using System.Text.Json.Serialization;

namespace ChromaDbClient.Models
{
	public class CreateCollection
	{
		[JsonPropertyName("get_or_create")]
		public bool GetOrCreate { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, object>? MetaData { get; set; } = new Dictionary<string, object>();

		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}