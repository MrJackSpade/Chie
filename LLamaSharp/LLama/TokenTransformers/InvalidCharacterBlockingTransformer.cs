using Llama;
using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System.Collections.Generic;
using System.Diagnostics;

namespace Llama.TokenTransformers
{
	public class InvalidCharacterBlockingTransformer : ITokenTransformer
	{
		public IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, SafeLlamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			foreach (LlamaToken token in selectedTokens)
			{
				if (token.Value == "�")
				{
					Debug.WriteLine($"Blocking token [{token.Id}]...");

					if (!settings.LogitBias.ContainsKey(token.Id))
					{
						settings.LogitBias.Add(token.Id, float.NegativeInfinity);
					}

					continue;
				}

				yield return token;
			}
		}
	}
}