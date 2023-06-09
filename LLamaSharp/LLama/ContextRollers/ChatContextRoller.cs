using LLama.Interfaces;
using LLama.Models;
using LLama.Native;
using System.Linq;

namespace LLama.ContextRollers
{
	public class ChatContextRoller : IContextRoller
	{
		public ContextState GenerateContext(SafeLLamaContext context, LlamaTokenCollection originalPrompt, LlamaTokenCollection history, int keepTokens)
		{
			ContextState toReturn = new();

			LlamaToken newline = context.GetToken(13);

			//Take half the context after the prompt
			int n_take = (context.Size - keepTokens) / 2;

			LlamaTokenCollection new_history = history.From(n_take, newline);

			toReturn.Tokens.AppendControl(NativeApi.llama_token_bos());

			LlamaTokenCollection[] keepLines = new_history.Split(newline.Id).Where(c => c.Count > 0).ToArray();

			//TODO: Make me a parameter. Dont allow 0;
			int lineBuffer = 5;
			if (keepLines.Length < lineBuffer)
			{
				toReturn.Tokens.Append(originalPrompt);
				toReturn.Tokens.Append(history);
			}
			else
			{
				for (int i = 0; i < keepLines.Length; i++)
				{
					int remaining = keepLines.Length - i;

					bool nl = i != 0;

					if (remaining == lineBuffer)
					{
						if (nl)
						{
							toReturn.Tokens.Append(newline);
						}

						toReturn.Tokens.Append(originalPrompt);

						nl = true;
					}

					if (nl)
					{
						toReturn.Tokens.Append(newline);
					}

					toReturn.Tokens.Append(keepLines[i]);
				}
			}

			return toReturn;
		}
	}
}