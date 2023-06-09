using LLama;
using LLama.Interfaces;
using LLama.Models;
using LLama.Native;
using System.Collections.Generic;

namespace ChieApi.TokenTransformers
{
	public class InteractiveEosReplace : ITokenTransformer
	{
		public IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, SafeLLamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			foreach (LlamaToken selectedToken in selectedTokens)
			{
				// replace end of text token with newline token when in interactive mode
				if (selectedToken.Id == NativeApi.llama_token_eos() && settings.Interactive && !settings.Instruct)
				{
					yield return context.GetToken(13);

					if (settings.Antiprompt.Count != 0)
					{
						// tokenize and inject first reverse prompt
						LlamaTokenCollection first_antiprompt = context.Tokenize(settings.Antiprompt[0]);

						foreach (LlamaToken token in first_antiprompt)
						{
							yield return token;
						}
					}
				} else
				{
					yield return selectedToken;
				}
			}
		}
	}
}