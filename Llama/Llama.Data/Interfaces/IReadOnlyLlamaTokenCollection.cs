using Llama.Data.Collections;
using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface IReadOnlyLlamaTokenCollection : IEnumerable<LlamaToken>
    {
        int Count { get; }

        IEnumerable<int> Ids { get; }

        bool IsNullOrWhiteSpace { get; }

        LlamaToken this[int index] { get; }

        LlamaTokenCollection Trim(int id = 0);
    }
}