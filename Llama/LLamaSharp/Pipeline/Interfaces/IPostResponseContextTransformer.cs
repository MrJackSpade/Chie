using Llama.Data;
using System.Collections.Generic;

namespace Llama.Pipeline.Interfaces
{
    public interface IPostResponseContextTransformer
    {
        public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated);
    }
}