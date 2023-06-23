using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	[Flags]
	public enum IncludeEnum
	{
		Documents,
		Embeddings,
		Metadatas,
		Distances
	}
}
