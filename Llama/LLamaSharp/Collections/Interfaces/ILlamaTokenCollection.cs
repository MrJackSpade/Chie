using Llama.Data;

namespace Llama.Collections.Interfaces
{
    public interface ILlamaTokenCollection : IReadOnlyLlamaTokenCollection
    {
        void Append(LlamaToken token);

        void AppendControl(int id);

        void Clear();

        LlamaTokenCollection Replace(LlamaTokenCollection toFind, LlamaTokenCollection toReplace);
    }
}