using ChromaDbClient.Interfaces;
using ChromaDbClient.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	public class QueryTexts
	{
		[JsonPropertyName("query_texts")]
		public List<string> Embeddings { get; set; } = new();

		[JsonPropertyName("where")]
		public IWhere? Where { get; set; }

		[JsonPropertyName("where_document")]
		public WhereDocument? WhereDocument { get; set; }

		[JsonPropertyName("n_results")]
		public int MaxResults { get; set; } = 20;
	}
}
