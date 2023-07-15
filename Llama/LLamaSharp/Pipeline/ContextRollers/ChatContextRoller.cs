using Llama.Collections;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Extensions;
using Llama.Native;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Llama.Pipeline.ContextRollers
{
    public class ChatContextRoller : IContextRoller
    {
        private readonly IBlockProcessor _summarizer;

        public ChatContextRoller(IBlockProcessor summarizer)
        {
            this._summarizer = summarizer;
        }

        public LlamaTokenCollection GenerateContext(IContext context, LlamaTokenCollection originalPrompt, int keepTokens)
        {
            LlamaToken splitNewLine = context.GetToken(13, LlamaTokenTags.UNMANAGED);

            List<LlamaTokenCollection> chatBlocks = this._summarizer.Finalize().ToList();

            LlamaTokenCollection toReturn = new();

            List<LlamaTokenCollection> new_history = chatBlocks.ToList();

            //toReturn.Append(LlamaToken.Bos);

            List<LlamaTokenCollection> keepLines = new();

            foreach (LlamaTokenCollection line in new_history.SelectMany(l => l.Split(splitNewLine.Id)))
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
                toReturn.Append(new LlamaTokenCollection(context.Buffer));
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

            this._summarizer.Process(toReturn);

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