using Llama.Collections.Interfaces;
using Llama.Context;
using Llama.Context.Interfaces;
using Llama.Data;
using System.Collections.Generic;

namespace Llama.Pipeline.Interfaces
{
    public interface ITokenTransformer
    {
        IEnumerable<LlamaToken> TransformToken(LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisCall, IEnumerable<LlamaToken> selectedTokens);
    }
}