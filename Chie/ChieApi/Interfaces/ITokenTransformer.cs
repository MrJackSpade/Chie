using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Interfaces
{
    public interface ITokenTransformer
    {
        IAsyncEnumerable<LlamaToken> TransformToken(IReadOnlyLlamaTokenCollection thisCall, IAsyncEnumerable<LlamaToken> selectedTokens);
    }
}
