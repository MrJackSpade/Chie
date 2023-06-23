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

        LlamaTokenCollection From(int startIndex, LlamaToken startToken);

        LlamaTokenCollection Replace(LlamaTokenCollection toFind, LlamaTokenCollection toReplace);
        LlamaTokenCollection Trim(int id = 0);
        void Ensure();
    }
}