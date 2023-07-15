using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface ILlamaTokenCollection : IReadOnlyLlamaTokenCollection
    {
        void Append(LlamaToken token);
    }
}