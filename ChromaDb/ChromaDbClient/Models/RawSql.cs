using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	public class RawSql
	{
		[JsonPropertyName("raw_sql")]
		public string Query { get; set; }
	}
}
