using Llama.Data;
using LlamaApi.Shared.Models.Request;
using LlamaApiClient;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChieApi.Clients
{
    public class RunpodClient : LlamaContextClient
    {
        private const string API_KEY = "Y2501TD117ZJ43A7OL428ZMWHG047IOFOXUSNMMA";

        private const string RUN_ASYNC = "https://api.runpod.ai/v2/5mnz7udeuuxn1t/run";

        private const string RUN_SYNC = "https://api.runpod.ai/v2/5mnz7udeuuxn1t/runsync";

        private const string STATUS = "https://api.runpod.ai/v2/5mnz7udeuuxn1t/status";

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

            ClientResponse hr = await WrapAndExecute(host, url);

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

            ClientResponse hr = await WrapAndExecute(url, "post", str_content);

            return hr;
        }

        public static JsonElement RemoveNullProperties(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    Dictionary<string, JsonElement> obj = new();
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Null)
                        {
                            obj[prop.Name] = RemoveNullProperties(prop.Value);
                        }
                    }

                    return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(obj));

                case JsonValueKind.Array:
                    List<JsonElement> array = new();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        array.Add(RemoveNullProperties(item));
                    }

                    return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(array));

                default:
                    return element;
            }
        }

        // To use this method with a JsonObject, first convert it to a JsonElement.
        public static JsonObject RemoveNullPropertiesFromJsonObject(JsonObject jsonObject)
        {
            JsonSerializerOptions options = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(jsonObject, options);
            JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

            JsonElement cleanedElement = RemoveNullProperties(jsonElement);
            return JsonSerializer.Deserialize<JsonObject>(cleanedElement.GetRawText(), options);
        }

        private async Task<ClientResponse> WrapAndExecute(string url, string method, string? body = null)
        {
            JsonObject json = new()
            {
                ["url"] = url,
                ["method"] = method
            };

            if (body != null)
            {
                json["body"] = JsonNode.Parse(body);
            }

            JsonObject wrapper = new()
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["input"] = json
            };

            wrapper = RemoveNullPropertiesFromJsonObject(wrapper);

            JsonContent newContent = JsonContent.Create(wrapper);

            HttpResponseMessage hr = await HttpClient.PostAsync(RUN_SYNC, newContent);

            string response = await hr.Content.ReadAsStringAsync();

            JsonObject responseObject = JsonNode.Parse(response) as JsonObject;

            string id = responseObject["id"]!.ToString();
            string status = responseObject["status"]!.ToString();

            while (status is "IN_QUEUE" or "IN_PROGRESS")
            {
                await Task.Delay(1000);

                hr = await HttpClient.GetAsync(STATUS + $"/{id}");

                response = await hr.Content.ReadAsStringAsync();

                responseObject = JsonNode.Parse(response) as JsonObject;

                status = responseObject["status"]!.ToString();
            }

            System.Diagnostics.Debug.WriteLine(responseObject["executionTime"].ToString());

            JsonObject output = (JsonObject)JsonNode.Parse(responseObject["output"].ToString());

            return new ClientResponse()
            {
                Status = int.Parse(output["status"].ToString()),
                Body = output["body"].ToString()
            };
        }
    }
}