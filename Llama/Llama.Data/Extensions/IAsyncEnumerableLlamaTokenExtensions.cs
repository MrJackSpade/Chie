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

        public static async Task<List<LlamaToken>> ToList(this IAsyncEnumerable<LlamaToken> enumerable)
        {
            List<LlamaToken> toReturn = new();

            await foreach (LlamaToken token in enumerable)
            {
                toReturn.Add(token);
            }

            return toReturn;
        }
    }
}
