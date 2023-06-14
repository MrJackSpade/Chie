using Llama.Constants;
using Llama.Interfaces;
using Llama.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Llama.PostResponseContextTransformers
{
	internal class RemoveTemporaryTokens : IPostResponseContextTransformer
	{
		public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated)
		{
			foreach (LlamaToken token in evaluated)
			{
				if(token.Tag != LlamaTokenTags.TEMPORARY)
				{
					yield return token;
				}
			}
		}
	}
}
