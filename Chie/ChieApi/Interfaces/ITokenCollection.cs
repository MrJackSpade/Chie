using ChieApi.Models;
using Llama.Data.Models;

namespace ChieApi.Interfaces
{
    public interface ITokenCollection : IAsyncEnumerable<LlamaToken>
    {
        long Id { get; }

        LlamaTokenType Type { get; }
    }
}