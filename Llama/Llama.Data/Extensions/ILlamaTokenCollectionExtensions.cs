using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace Llama.Data.Extensions
{
    public static class ILlamaTokenCollectionExtensions
    {
        public static void Append(this ILlamaTokenCollection target, IEnumerable<LlamaToken> tokens)
        {
            foreach (LlamaToken token in tokens)
            {
                target.Append(token);
            }
        }
    }
}
