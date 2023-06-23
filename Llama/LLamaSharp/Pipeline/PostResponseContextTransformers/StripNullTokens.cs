using Llama.Data;
using Llama.Pipeline.Interfaces;
using System.Collections.Generic;

namespace Llama.Pipeline.PostResponseContextTransformers
{
    public class StripNullTokens : IPostResponseContextTransformer
    {
        public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated)
        {
            foreach (LlamaToken token in evaluated)
            {
                if (token.Id != 0)
                {
                    yield return token;
                }
            }
        }
    }
}