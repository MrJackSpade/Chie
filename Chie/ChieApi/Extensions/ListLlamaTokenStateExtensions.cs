using ChieApi.Models;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Extensions
{
    public static class ListLlamaTokenStateExtensions
    {
        public static LlamaTokenCollection ToCollection(this List<LlamaTokenState> source)
        {
            LlamaTokenCollection collection = new();

            foreach (LlamaTokenState item in source)
            {
                collection.Append(new LlamaToken(item.Id, item.Value));
            }

            return collection;
        }

        public static List<LlamaTokenState> ToStateList(this IReadOnlyLlamaTokenCollection source)
        {
            List<LlamaTokenState> collection = new();

            foreach (LlamaToken item in source)
            {
                collection.Add(new LlamaTokenState()
                {
                    Id = item.Id,
                    Value = item.Value
                });
            }

            return collection;
        }

        public static async Task<List<LlamaTokenState>> ToStateList(this IAsyncEnumerable<LlamaToken> source)
        {
            List<LlamaTokenState> collection = new();

            await foreach (LlamaToken item in source)
            {
                collection.Add(new LlamaTokenState()
                {
                    Id = item.Id,
                    Value = item.Value
                });
            }

            return collection;
        }
    }
}