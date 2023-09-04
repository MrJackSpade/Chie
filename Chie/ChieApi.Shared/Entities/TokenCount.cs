using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChieApi.Shared.Entities
{
	public class TokenCount
	{
		public long ChatEntryId { get; set; }
		public int Count { get;set; }
		public int ModelId { get; set; }
	}
}
