using LLama;
using LLama.Interfaces;
using LLama.Models;
using LLama.Native;

namespace Llama.TokenTransformers
{
	public class NewlineEnsureTransformer : ITokenTransformer
	{
		private readonly List<string> _antiprompts = new();

		private readonly List<LlamaTokenCollection> _antiPromptTokens = new();

		private LlamaToken _newLineToken = null;

		public NewlineEnsureTransformer(IEnumerable<string> allReversePrompts)
		{
			_antiprompts.AddRange(allReversePrompts.Where(a => a != "\n"));
		}

		public IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, SafeLLamaContext context, IEnumerable<LlamaToken> selectedTokens)
		{
			LlamaTokenCollection input = new(selectedTokens);

			if (this._antiPromptTokens.Count != this._antiprompts.Count)
			{
				this._newLineToken = context.GetToken(13);

				foreach (string antiPrompt in this._antiprompts)
				{
					this._antiPromptTokens.Add(context.Tokenize(antiPrompt));
				}
			}

			foreach (LlamaTokenCollection antiPromptTokenCollection in this._antiPromptTokens)
			{
				input = input.Replace(antiPromptTokenCollection, this._newLineToken + antiPromptTokenCollection);
			}

			foreach (LlamaToken token in input)
			{
				yield return token;
			}
		}
	}
}