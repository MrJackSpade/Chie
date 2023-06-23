using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	public class GetResponse : BaseResponse
	{
		public List<string> Ids { get; } = new ();
		public List<float> Embeddings { get; } = new ();
		public List<string> Documents { get; } = new ();
		public List<Dictionary<string, object>> Metadatas { get; } = new ();
		public string? Error { get; set; }
	}
}
