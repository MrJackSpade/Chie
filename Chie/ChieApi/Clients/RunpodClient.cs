using Llama.Data;
using LlamaApi.Shared.Models.Request;
using LlamaApiClient;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChieApi.Clients
{
    public class RunpodClient : LlamaClient
    {
        private const string API_KEY = "TJYYVDPEFW5H5Z33G9XZATVN5723N8PU1UD0VDMG";

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

        public override async Task<ClientResponse> PostAsync(string host, string url, string content)
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

            ClientResponse hr = await WrapAndExecute(url, "post", content);

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
            body = body.Trim();

            JsonObject json = new()
            {
                ["url"] = url,
                ["method"] = method
            };

            if (body != null)
            {
                if(body.StartsWith("{") && body.EndsWith("}"))
                {
                    json["body"] = JsonNode.Parse(body);
                }
                else
                {
                    json["body"] = body;
                }
            }

            JsonObject wrapper = new()
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["input"] = json
            };

            wrapper = RemoveNullPropertiesFromJsonObject(wrapper);

            JsonContent newContent = JsonContent.Create(wrapper);

            Stopwatch requestStopwatch = Stopwatch.StartNew();

            HttpResponseMessage hr = await HttpClient.PostAsync(RUN_SYNC, newContent);

            string response = await hr.Content.ReadAsStringAsync();

            JsonObject responseObject = JsonNode.Parse(response) as JsonObject;

            string id = responseObject["id"]!.ToString();
            string status = responseObject["status"]!.ToString();

            while (status is "IN_QUEUE" or "IN_PROGRESS")
            {
                await Task.Delay(50);

                hr = await HttpClient.GetAsync(STATUS + $"/{id}");

                response = await hr.Content.ReadAsStringAsync();

                responseObject = JsonNode.Parse(response) as JsonObject;

                status = responseObject["status"]!.ToString();
            }

            requestStopwatch.Stop();

            string debugLog = $"[{DateTime.Now:HH:mm:ss}] RequestTime: {requestStopwatch.ElapsedMilliseconds:N0}ms; DelayTime: {int.Parse(responseObject["delayTime"].ToString()):N0}; ExecutionTime: {int.Parse(responseObject["executionTime"].ToString()):N0}ms";
            
            System.Diagnostics.Debug.WriteLine(debugLog);

            JsonObject output = (JsonObject)JsonNode.Parse(responseObject["output"].ToString());

            return new ClientResponse()
            {
                Status = int.Parse(output["status"].ToString()),
                Body = output["body"].ToString()
            };
        }
    }
}