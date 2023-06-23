using Llama.Constants;
using Llama.Data;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;

namespace Llama.Pipeline.PostResponseContextTransformers
{
    public class RemoveTemporaryTokens : IPostResponseContextTransformer
    {
        public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated)
        {
            foreach (LlamaToken token in evaluated)
            {
                if (token.Tag != LlamaTokenTags.TEMPORARY)
                {
                    yield return token;
                }
            }
        }
    }
}