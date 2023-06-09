using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Exceptions
{
	public class ChromaDbException : Exception
	{
		public ChromaDbException(string? message) : base(message)
		{
		}
	}
}
