using Llama.Collections;
using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Llama.Extensions
{
	public static class IEnumerableIPostResponseContextTransformerExtensions
	{
		public static IEnumerable<LlamaToken> Transform(this IEnumerable<IPostResponseContextTransformer> tokenTransformers, IEnumerable<LlamaToken> buffer)
		{
			IEnumerable<LlamaToken> returnTokens = buffer;

			foreach (IPostResponseContextTransformer tokenTransformer in tokenTransformers)
			{
				returnTokens = tokenTransformer.Transform(returnTokens);
			}

			return returnTokens;
		}
	}
}
