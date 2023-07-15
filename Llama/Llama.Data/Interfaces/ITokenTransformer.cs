using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface ITokenTransformer
    {
        IEnumerable<LlamaToken> TransformToken(LlamaContextSettings settings, IContext context, IReadOnlyLlamaTokenCollection thisCall, IEnumerable<LlamaToken> selectedTokens);
    }
}