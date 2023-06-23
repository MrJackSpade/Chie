using Llama.Collections;
using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Native;
using Llama.Pipeline.Interfaces;

namespace Llama.TokenTransformers
{
    public class NewlineEnsureTransformer : ITokenTransformer
    {
        private readonly int _newlineTokenId = -1;

        private bool _lastNewLine = false;

        public NewlineEnsureTransformer()
        {
            this._newlineTokenId = NativeApi.llama_token_nl();
        }

        public IEnumerable<LlamaToken> TransformToken(LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisGeneration, IEnumerable<LlamaToken> selectedTokens)
        {
            LlamaTokenCollection input = new();

            foreach (LlamaToken token in selectedTokens)
            {
                if (token.Id == 30004) //\r
                {
                    input.Append(context.GetToken(this._newlineTokenId, token.Tag));
                }
                else
                {
                    input.Append(token);
                }
            }

            string inputString = input.ToString();

            if (inputString.IndexOf("|") == 0)
            {
                if (!this._lastNewLine)
                {
                    this._lastNewLine = true;
                    yield return context.GetToken(this._newlineTokenId, LlamaTokenTags.UNMANAGED);
                }
            }
            else
            {
                foreach (LlamaToken token in input)
                {
                    if (!this._lastNewLine || token.Id != this._newlineTokenId)
                    {
                        yield return token;
                    }

                    this._lastNewLine = token.Id == this._newlineTokenId;
                }
            }
        }
    }
}