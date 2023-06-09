using LLama.Interfaces;
using LLama.Models;
using LLama.Native;
using System.Collections.Generic;

namespace LLama.Extensions
{
	public static class IEnumerableITokenTransformerExtensions
	{
		public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, LlamaModelSettings settings, SafeLLamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			IEnumerable<LlamaToken> returnTokens = selectedTokens;

			foreach (ITokenTransformer tokenTransformer in tokenTransformers)
			{
				returnTokens = tokenTransformer.TransformToken(settings, context, returnTokens);
			}

			return returnTokens;
		}

		public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, LlamaModelSettings settings, SafeLLamaContext context, LlamaToken selectedToken) => tokenTransformers.Transform(settings, context, new List<LlamaToken>() { selectedToken });
	}
}
