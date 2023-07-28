using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.Interfaces
{
    public interface ITokenTransformer
    {
        IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens);
    }
}