using Llama.Collections;
using Llama.Collections.Interfaces;
using System.Collections.Generic;

namespace Llama.Pipeline.Interfaces
{
    public interface IBlockProcessor
    {
        IEnumerable<LlamaTokenCollection> Finalize();

        void Process(ILlamaTokenCollection toSummarize);
    }
}