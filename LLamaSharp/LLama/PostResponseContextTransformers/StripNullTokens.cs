using Llama.Constants;
using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace Llama.PostResponseContextTransformers
{
	internal class StripNullTokens : IPostResponseContextTransformer
	{
		public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated)
		{
			foreach (LlamaToken token in evaluated)
			{
				if(token.Id != 0)
				{
					yield return token;
				}
			}
		}
	}
}
