using System.Text.Json.Serialization;

namespace ChromaDbClient.Models
{
	public class AddEmbedding
	{
		[JsonPropertyName("documents")]
		public List<string> Documents { get; set; } = new();

		[JsonPropertyName("embeddings")]
		public List<List<float>> Embeddings { get; set; } = new();

		[JsonPropertyName("ids")]
		public List<string> Ids { get; set; } = new();

		[JsonPropertyName("increment_index")]
		public bool IncrementIndex { get; set; }

		[JsonPropertyName("metadatas")]
		public List<Dictionary<string, object>> Metadatas { get; set; } = new();
	}
}