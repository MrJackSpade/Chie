using Llama.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Events
{
	public class LlameClientTokenGeneratedEventArgs : EventArgs
	{
		public LlamaToken Token { get; set; }
	}
}
