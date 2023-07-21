using Llama.Data.Collections;
using Llama.Data.Interfaces;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Models.Response;
using LlamaApiClient;
using System.Runtime.CompilerServices;

namespace ChieApi.Extensions
{
    public static class LlamaClientExtensions
    {
        public static async Task<ContextState> Eval(this LlamaContextClient client, IReadOnlyLlamaTokenCollection collection, int startIndex = -1)
        {
            ContextState state = await Write(client, collection, startIndex);
            await client.Eval();
            return state;
        }

        public static Task<ContextState> Write(this LlamaContextClient client, IReadOnlyLlamaTokenCollection collection, int startIndex = -1)
        {
            return client.Write(collection.Select(t => new RequestLlamaToken()
            {
                TokenId = t.Id
            }), startIndex);
        }
    }
}
