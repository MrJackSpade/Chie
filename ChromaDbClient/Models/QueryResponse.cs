namespace ChromaDbClient.Models
{
	public class QueryResponse : BaseResponse
	{
		public List<List<float>> Distances { get; } = new();

		public List<List<string>> Documents { get; } = new();

		public List<List<float>> Embeddings { get; } = new();

		public List<List<string>> Ids { get; } = new();

		public List<Dictionary<string, object>> Metadatas { get; } = new();
	}
}