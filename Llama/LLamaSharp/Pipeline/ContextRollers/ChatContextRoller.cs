using Llama.Collections;
using Llama.Constants;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Extensions;
using Llama.Native;
using System.Collections.Generic;
using System.Linq;

namespace Llama.Pipeline.ContextRollers
{
    public class ChatContextRoller : IContextRoller
    {
        public LlamaTokenCollection GenerateContext(IContext context, LlamaTokenCollection originalPrompt, int keepTokens)
        {
            LlamaTokenCollection existingContext = new(context.Buffer);

            LlamaTokenCollection toReturn = new();

            LlamaToken splitNewLine = context.GetToken(13, LlamaTokenTags.UNMANAGED);

            //Take half the context after the prompt
            int n_take = (int)((context.Size - keepTokens) * 0.75f);

            LlamaTokenCollection new_history = existingContext.From(n_take, splitNewLine);

            toReturn.AppendControl(NativeApi.llama_token_bos());

            List<LlamaTokenCollection> keepLines = new();

            foreach (LlamaTokenCollection line in new_history.Split(splitNewLine.Id))
            {
                if (line.Count == 0)
                {
                    continue;
                }

                if (line.IsSingleLlamaTokenTag && line.LlamaTokenTags.Single() == LlamaTokenTags.PROMPT)
                {
                    continue;
                }

                keepLines.Add(line);
            }

            //TODO: Make me a parameter. Dont allow 0;
            int lineBuffer = 5;
            if (keepLines.Count < lineBuffer)
            {
                toReturn.Append(originalPrompt);
                toReturn.Append(existingContext);
            }
            else
            {
                for (int i = 0; i < keepLines.Count; i++)
                {
                    int remaining = keepLines.Count - i;

                    bool nl = i != 0;

                    if (remaining == lineBuffer)
                    {
                        this.AppendNewline(context, toReturn);
                        toReturn.Append(originalPrompt);

                        nl = true;
                    }

                    if (nl)
                    {
                        this.AppendNewline(context, toReturn);
                    }

                    toReturn.Append(keepLines[i]);
                }
            }

            toReturn.Ensure();

            return toReturn;
        }

        private void AppendNewline(IContext context, LlamaTokenCollection tokens)
        {
            if (tokens.Count == 0)
            {
                tokens.Append(context.GetToken(13, LlamaTokenTags.UNMANAGED));
                return;
            }
            else if (tokens.Last().Id == 13)
            {
                return;
            }

            LlamaToken lastToken = tokens[^1];
            tokens.Append(context.GetToken(13, lastToken.Tag));
        }
    }
}