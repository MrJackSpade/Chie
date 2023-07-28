using ChieApi.Models;
using Llama.Data.Models;

namespace ChieApi.Interfaces
{
    public interface ITokenCollection : IAsyncEnumerable<LlamaToken>
    {
        LlamaTokenType Type { get; }
    }
}