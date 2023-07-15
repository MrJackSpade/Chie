using Llama.Collections;
using Llama.Collections.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Llama.Pipeline.Interfaces
{
    public interface IBlockProcessor
    {
        IEnumerable<LlamaTokenCollection> Finalize();

        Task Process(ILlamaTokenCollection toSummarize);
    }
}