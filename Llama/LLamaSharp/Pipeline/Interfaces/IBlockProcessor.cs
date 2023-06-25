using Llama.Collections;
using Llama.Collections.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Pipeline.Interfaces
{
    public interface IBlockProcessor
    {
        IEnumerable<LlamaTokenCollection> Finalize();
        void Process(ILlamaTokenCollection toSummarize);
    }
}
