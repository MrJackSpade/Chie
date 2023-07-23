using Llama.Data.Collections;
using Llama.Data.Models;

namespace Llama.Data.Extensions
{
    public static class IAsyncEnumerableLlamaTokenExtensions
    {
        public static async Task<LlamaTokenCollection> ToCollection(this IAsyncEnumerable<LlamaToken> enumerable)
        {
            LlamaTokenCollection toReturn = new();

            await foreach (LlamaToken token in enumerable)
            {
                toReturn.Append(token);
            }

            return toReturn;
        }
    }
}
