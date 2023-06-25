using Llama.Data;

namespace Llama.Collections.Interfaces
{
    public interface ILlamaTokenCollection : IReadOnlyLlamaTokenCollection
    {
        void Append(LlamaToken token);
    }
}