using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System.Collections.Generic;

namespace Llama.Extensions
{
	public static class IEnumerableITokenTransformerExtensions
	{
		public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, LlamaModelSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, SafeLlamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			IEnumerable<LlamaToken> returnTokens = selectedTokens;

			foreach (ITokenTransformer tokenTransformer in tokenTransformers)
			{
				returnTokens = tokenTransformer.TransformToken(settings, thisGeneration, context, returnTokens);
			}

			return returnTokens;
		}

		public static IEnumerable<LlamaToken> Transform(this IEnumerable<ITokenTransformer> tokenTransformers, LlamaModelSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, SafeLlamaContext context, LlamaToken selectedToken) => tokenTransformers.Transform(settings, thisGeneration, context, new List<LlamaToken>() { selectedToken });
	}
}
