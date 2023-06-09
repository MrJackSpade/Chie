using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	public class CollectionInformation : BaseResponse
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }
		[JsonPropertyName("id")]
		public string? Id { get; set; }
		[JsonPropertyName("metadata")]
		public Dictionary<string, object>? MetaData { get; set; } = new Dictionary<string, object>();
	}
}
