using Embedding.Interfaces;
using Embedding.Models;
using Loxifi;

namespace Embedding
{
    public class EmbeddingApiClient : IEmbeddingApiClient
    {
        private readonly EmbeddingApiClientSettings _settings;

        public EmbeddingApiClient(EmbeddingApiClientSettings settings)
        {
            this._settings = settings;
        }

        public async Task<EmbeddingResponse> Generate(string[] data)
        {
            return await this.Generate(new EmbeddingRequest()
            {
                TextData = data
            });
        }

        public async Task<EmbeddingResponse> Generate(EmbeddingRequest request)
        {
            JsonClient client = new();

            string url = $"{this._settings.RootUrl}/Embedding/Generate";

            EmbeddingResponse response = await client.PostJsonAsync<EmbeddingResponse>(url, request);

            return response;
        }
    }
}