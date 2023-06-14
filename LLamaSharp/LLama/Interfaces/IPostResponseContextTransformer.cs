using Llama.Collections;
using Llama.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Llama.Interfaces
{
	public interface IPostResponseContextTransformer
	{
		public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated);
	}
}
