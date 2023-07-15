using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface IPostResponseContextTransformer
    {
        public IEnumerable<LlamaToken> Transform(IEnumerable<LlamaToken> evaluated);
    }
}