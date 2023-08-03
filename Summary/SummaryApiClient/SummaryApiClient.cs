using Summary.Interfaces;
using Summary.Models;
using Loxifi;

namespace Summary
{
    public class SummaryApiClient : ISummaryApiClient
    {
        private readonly SummaryApiClientSettings _settings;

        public SummaryApiClient(SummaryApiClientSettings settings)
        {
            this._settings = settings;
        }

        public async Task<SummaryResponse> Summarize(string data, int maxLength = 512)
        {
            return await this.Summarize(new SummaryRequest()
            {
                TextData = data,
                MaxLength = maxLength
            });
        }

        public async Task<SummaryResponse> Summarize(SummaryRequest request)
        {
            JsonClient client = new();

            string url = $"{this._settings.RootUrl}/Text/Summarize";

            SummaryResponse response = await client.PostJsonAsync<SummaryResponse>(url, request);

            return response;
        }

        public async Task<TokenizeResponse> Tokenize(string data)
        {
            return await this.Tokenize(new TokenizeRequest()
            {
                TextData = data
            });
        }

        public async Task<TokenizeResponse> Tokenize(TokenizeRequest request)
        {
            JsonClient client = new();

            string url = $"{this._settings.RootUrl}/Text/Tokenize";

            TokenizeResponse response = await client.PostJsonAsync<TokenizeResponse>(url, request);

            return response;
        }
    }
}