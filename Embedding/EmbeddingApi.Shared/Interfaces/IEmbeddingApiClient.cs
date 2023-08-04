using Embedding.Models;

namespace Embedding.Interfaces
{
    public interface IEmbeddingApiClient
    {
        Task<EmbeddingResponse> Generate(string[] data);
    }
}