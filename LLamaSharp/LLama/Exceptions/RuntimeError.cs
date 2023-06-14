using System;

namespace Llama.Exceptions
{
	public class RuntimeError : Exception
	{
		public RuntimeError()
		{
		}

		public RuntimeError(string message) : base(message)
		{
		}
	}
}