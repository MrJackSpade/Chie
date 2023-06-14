using Llama;
using Llama.Collections;
using Llama.Constants;
using Llama.Interfaces;
using Llama.Models;
using Llama.Native;

namespace Llama.TokenTransformers
{
	public class NewlineEnsureTransformer : ITokenTransformer
	{

		private readonly int _newlineTokenId = -1;

		private bool _lastNewLine = false;
		public NewlineEnsureTransformer()
		{
			_newlineTokenId = NativeApi.llama_token_nl();
		}

		public IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, SafeLlamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			LlamaTokenCollection input = new(selectedTokens);

			string inputString = input.ToString();

			if (inputString.IndexOf("|") == 0)
			{
				if (!_lastNewLine)
				{
					_lastNewLine = true;
					yield return context.GetToken(_newlineTokenId, LlamaTokenTags.UNMANAGED);
				}
			}
			else
			{
				foreach (LlamaToken token in input)
				{
					if (!_lastNewLine || token.Id != _newlineTokenId)
					{
						yield return token;
					}

					_lastNewLine = token.Id == _newlineTokenId;
				}
			}
		}
	}
}