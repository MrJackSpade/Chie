using ChromaDbClient.Interfaces;
using ChromaDbClient.Operators;
using System.Text.Json.Serialization;

namespace ChromaDbClient.Models
{
	public class GetEmbedding
	{
		[JsonPropertyName("ids")]
		public List<string> Ids { get; set; } = new();

		[JsonPropertyName("include")]
		public List<IncludeEnum> Include { get; set; } = new();

		[JsonPropertyName("limit")]
		public int Limit { get; set; }

		[JsonPropertyName("offset")]
		public int Offset { get; set; }

		[JsonPropertyName("sort")]
		public string? Sort { get; set; }

		[JsonPropertyName("where")]
		public IWhere? Where { get; set; }

		[JsonPropertyName("where_document")]
		public WhereDocument? WhereDocument { get; set; }
	}
}