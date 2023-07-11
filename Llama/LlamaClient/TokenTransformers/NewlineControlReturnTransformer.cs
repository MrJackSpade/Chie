using Llama.Collections.Interfaces;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Pipeline.Interfaces;
using System.Text.RegularExpressions;

namespace Llama.TokenTransformers
{
    public class NewlineControlReturnTransformer : ITokenTransformer
    {
        private readonly string _continueChars = ",:";

        private const string LIST_REGEX = @"^\s*\d*\.";
        public NewlineControlReturnTransformer()
        {

        }

        public bool ShouldContinue(IReadOnlyLlamaTokenCollection thisCall)
        {
            string toTest = thisCall.ToString().Trim();

            foreach(char c in _continueChars)
            {
                if (toTest.EndsWith(c))
                {
                    return true;
                }
            }

            string lastLine = toTest.Split('\n').Last();

            if(Regex.IsMatch(lastLine, LIST_REGEX))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<LlamaToken> TransformToken(LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisCall, IEnumerable<LlamaToken> selectedTokens)
        {
            foreach(LlamaToken token in selectedTokens)
            {
                if(token.Id != 13 || this.ShouldContinue(thisCall))
                    {
                        yield return token;
                    } else
                {
                    yield return LlamaToken.EOS;
                    yield break;
                }
            }
        }
    }
}