using ChromaDbClient.Interfaces;
using ChromaDbClient.Operators;
using System.Text.Json.Serialization;

namespace ChromaDbClient.Models
{
	public class DeleteEmbedding
	{
		[JsonPropertyName("ids")]
		public List<string> Ids { get; set; } = new();

		[JsonPropertyName("where")]
		public IWhere? Where { get; set; }

		[JsonPropertyName("where_document")]
		public WhereDocument? WhereDocument { get; set; }

		public DeleteEmbedding()
		{

		}

		public DeleteEmbedding(params string[] ids)
		{
			this.Ids = ids.ToList();
		}
	}
}
