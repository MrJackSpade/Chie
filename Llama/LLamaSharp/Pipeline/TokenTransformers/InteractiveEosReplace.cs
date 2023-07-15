using Llama.Collections;
using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Native;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;

namespace Llama.Pipeline.TokenTransformers
{
    public class InteractiveEosReplace : ITokenTransformer
    {
        public IEnumerable<LlamaToken> TransformToken(Context.LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisCall, IEnumerable<LlamaToken> selectedTokens)
        {
            foreach (LlamaToken selectedToken in selectedTokens)
            {
                bool isEos = selectedToken.Id == NativeApi.TokenEos();

                // replace end of text token with newline token when in interactive mode
                if (isEos && settings.Interactive && !settings.Instruct)
                {
                    yield return context.GetToken(13, LlamaTokenTags.RESPONSE);

                    if (settings.Antiprompt.Count != 0)
                    {
                        string firstAnti = settings.Antiprompt[0];

                        if (firstAnti != "\n")
                        {
                            // tokenize and inject first reverse prompt
                            LlamaTokenCollection first_antiprompt = context.Tokenize(firstAnti, LlamaTokenTags.RESPONSE);

                            foreach (LlamaToken token in first_antiprompt)
                            {
                                yield return token;
                            }
                        }
                    }
                }
                else
                {
                    yield return selectedToken;
                }
            }
        }
    }
}