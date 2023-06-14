using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System.Collections.Generic;

namespace Llama.Extensions
{
	public static class IEnumerableITokenTransformerExtensions
	{
		public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, LlamaModelSettings settings, SafeLlamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			IEnumerable<LlamaToken> returnTokens = selectedTokens;

			foreach (ITokenTransformer tokenTransformer in tokenTransformers)
			{
				returnTokens = tokenTransformer.TransformToken(settings, context, returnTokens);
			}

			return returnTokens;
		}

		public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, LlamaModelSettings settings, SafeLlamaContext context, LlamaToken selectedToken) => tokenTransformers.Transform(settings, context, new List<LlamaToken>() { selectedToken });
	}
}
