using Llama.Data;
using LlamaApi.Shared.Models.Request;
using LlamaApiClient;
using System.Text.Json.Nodes;

namespace ChieApi.Clients
{
    public class RunpodClient : LlamaContextClient
    {
        private const string API_KEY = "Y2501TD117ZJ43A7OL428ZMWHG047IOFOXUSNMMA";

        private const string RUN_ASYNC = "https://api.runpod.ai/v2/5mnz7udeuuxn1t/run";

        private const string RUN_SYNC = "https://api.runpod.ai/v2/5mnz7udeuuxn1t/runsync";

        public RunpodClient(LlamaClientSettings settings, LlamaContextSettings contextSettings, LlamaModelSettings modelSettings, ContextRequestSettings contextRequestSettings) : base(settings, contextSettings, modelSettings, contextRequestSettings)
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
        }

        public override async Task<ClientResponse> GetAsync(string host, string url)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException($"'{nameof(host)}' cannot be null or empty.", nameof(host));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
            }

            HttpResponseMessage hr = await WrapAndExecute(host, url);

            return hr;
        }

        public override async Task<ClientResponse> PostAsync(string host, string url, JsonContent content)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException($"'{nameof(host)}' cannot be null or empty.", nameof(host));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            string str_content = await content.ReadAsStringAsync()!;

            HttpResponseMessage hr = await WrapAndExecute(url, "post", str_content);

            return hr;
        }

        private async Task<ClientResponse> WrapAndExecute(string url, string method, string? body = null)
        {
            JsonObject json = new();

            json["url"] = url;
            json["method"] = method;

            if (body != null)
            {
                json["body"] = JsonNode.Parse(body);
            }

            JsonObject wrapper = new();

            wrapper["id"] = Guid.NewGuid().ToString();
            wrapper["input"] = json;

            JsonContent newContent = JsonContent.Create(wrapper);

            HttpResponseMessage hr = await HttpClient.PostAsync(RUN_SYNC, newContent);

            string response = await hr.Content.ReadAsStringAsync();

            JsonObject resposneObject = JsonNode.Parse(response) as JsonObject;

            return hr;
        }
    }
}