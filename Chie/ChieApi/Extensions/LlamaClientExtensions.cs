using Llama.Data.Collections;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApiClient;
using System.Runtime.CompilerServices;

namespace ChieApi.Extensions
{
    public static class LlamaClientExtensions
    {
        public static Task<ContextState> Write(this LlamaContextClient client, LlamaTokenCollection collection, int startIndex = -1)
        {
            return client.Write(collection.Select(t => new RequestLlamaToken()
            {
                TokenId = t.Id
            }), startIndex);
        }
    }
}
