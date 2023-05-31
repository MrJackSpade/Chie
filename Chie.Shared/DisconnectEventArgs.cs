using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Shared
{
	public class DisconnectEventArgs
	{
		public uint ResultCode { get; set; }
		public string RollOverPrompt { get; set; }
	}
}
