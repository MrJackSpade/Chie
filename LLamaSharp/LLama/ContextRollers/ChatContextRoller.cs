﻿using Llama.Collections;
using Llama.Constants;
using Llama.Extensions;
using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System.Linq;

namespace Llama.ContextRollers
{
    public class ChatContextRoller : IContextRoller
	{
		public LlamaTokenCollection GenerateContext(SafeLlamaContext context, LlamaTokenCollection queue, LlamaTokenCollection originalPrompt, int keepTokens)
		{
			LlamaTokenCollection existingContext = new(context.Buffer);
			existingContext.Append(queue);

			LlamaTokenCollection toReturn = new();

			LlamaToken newline = context.GetToken(13, LlamaTokenTags.UNMANAGED);

			//Take half the context after the prompt
			int n_take = (context.Size - keepTokens) / 2;

			LlamaTokenCollection new_history = existingContext.From(n_take, newline);

			toReturn.AppendControl(NativeApi.llama_token_bos());

			LlamaTokenCollection[] keepLines = new_history.Split(newline.Id).Where(c => c.Count > 0).ToArray();

			//TODO: Make me a parameter. Dont allow 0;
			int lineBuffer = 5;
			if (keepLines.Length < lineBuffer)
			{
				toReturn.Append(originalPrompt);
				toReturn.Append(existingContext);
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
							toReturn.Append(newline);
						}

						toReturn.Append(originalPrompt);

						nl = true;
					}

					if (nl)
					{
						toReturn.Append(newline);
					}

					toReturn.Append(keepLines[i]);
				}
			}

			return toReturn;
		}
	}
}