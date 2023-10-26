using Llama.Data.Models;
using LlamaApiClient;

namespace LlamaApi.Shared.Interfaces
{
    public interface ITokenTransformer
    {
        IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens);
    }
}