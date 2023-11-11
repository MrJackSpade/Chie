using Llama.Data.Interfaces;
using LlamaApi.Models.Request;
using LlamaApi.Shared.Models.Response;
using LlamaApiClient;

namespace ChieApi.Extensions
{
    public static class LlamaClientExtensions
    { 
        public static Task Write(this LlamaClient client, IReadOnlyLlamaTokenCollection collection, int startIndex = -1)
        {
            return client.Write(collection.Select(t => new RequestLlamaToken()
            {
                TokenId = t.Id
            }), startIndex);
        }
    }
}