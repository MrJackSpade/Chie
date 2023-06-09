using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	public class UpdateCollection
	{
		[JsonPropertyName("new_name")]
		public string NewName { get; set; }
		[JsonPropertyName("new_metadata")]
		public Dictionary<string, object>? NewMetaData { get; set; } = new Dictionary<string, object>();
	}
}
