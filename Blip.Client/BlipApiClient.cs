using Blip.Shared.Interfaces;
using Blip.Shared.Models;
using Loxifi;

namespace Blip.Client
{
    public class BlipApiClient : IBlipApiClient
    {
        private readonly BlipApiClientSettings _settings;

        public BlipApiClient(BlipApiClientSettings settings)
        {
            this._settings = settings;
        }

        public async Task<DescribeResponse> Describe(byte[] data)
        {
            return await this.Describe(new DescribeRequest()
            {
                FileData = data
            });
        }

        public async Task<DescribeResponse> Describe(string filePath)
        {
            return await this.Describe(new DescribeRequest()
            {
                FilePath = filePath
            });
        }

        public async Task<DescribeResponse> Describe(DescribeRequest request)
        {
            JsonClient client = new();

            string url = $"{this._settings.RootUrl}/Image/Describe";

            DescribeResponse response = await client.PostJsonAsync<DescribeResponse>(url, request);

            return response;
        }
    }
}