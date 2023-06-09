using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Models
{
	public class DeletedResponse : BaseResponse
	{
		public List<string> Ids { get; set; } = new List<string>();
	}
}
