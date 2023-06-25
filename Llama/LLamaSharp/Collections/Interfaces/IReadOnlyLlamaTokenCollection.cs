using Llama.Data;
using System.Collections.Generic;

namespace Llama.Collections.Interfaces
{
    public interface IReadOnlyLlamaTokenCollection : IEnumerable<LlamaToken>
    {
        int Count { get; }

        IEnumerable<int> Ids { get; }

        bool IsNullOrWhiteSpace { get; }

        LlamaToken this[int index] { get; }

        void Ensure();

        LlamaTokenCollection Trim(int id = 0);
    }
}