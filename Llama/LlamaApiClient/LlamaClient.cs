using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Converters;
using LlamaApi.Shared.Models;
using LlamaApi.Shared.Models.Request;
using LlamaApi.Shared.Models.Response;
using LlamaApi.Shared.Serializers;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaApiClient
{
    public class LlamaClient
    {
        private readonly ContextRequestSettings _contextRequestSettings;

        private readonly LlamaContextSettings _contextSettings;

        private readonly Guid _modelGuid = Guid.Empty;

        private readonly LlamaModelSettings _modelSettings;

        private readonly JsonSerializerOptions _serializerOptions;

        private readonly LlamaClientSettings _settings;

        public LlamaClient(LlamaClientSettings settings, LlamaContextSettings contextSettings, LlamaModelSettings modelSettings, ContextRequestSettings contextRequestSettings)
        {
            _settings = settings;

            HttpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromDays(1)
            };

            _serializerOptions = new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            _serializerOptions.Converters.Add(new LogitRuleConverter());
            _contextSettings = contextSettings;
            _modelSettings = modelSettings;
            _contextRequestSettings = contextRequestSettings;
        }

        protected HttpClient HttpClient { get; }

        public async Task DisposeContext(Guid contextId)
        {
            await Post("/Llama/context/dispose", new ContextDisposeRequest()
            {
                ContextId = contextId
            }, contextId);
        }

        public async Task Eval(Guid contextId)
        {
            EvaluationResponse response = await PostCollection<EvaluationResponse>("/Llama/eval", new EvaluateRequest()
            {
                ContextId = contextId
            }, contextId);

            Debug.WriteLine("Evaluated: " + response.Evaluated);
        }

        public virtual async Task<ClientResponse> GetAsync(string host, string url)
        {
            HttpResponseMessage hr = await HttpClient.GetAsync(host + url);

            return new ClientResponse()
            {
                Body = await hr.Content.ReadAsStringAsync(),
                Status = (int)hr.StatusCode
            };
        }

        public async Task<float[]> GetLogits(Guid contextId)
        {
            GetLogitsRequest request = new()
            {
                ContextId = contextId
            };

            GetLogitsResponse response = await PostCollection<GetLogitsResponse>("/Llama/getlogits", request, contextId);

            return response.GetValue().ToArray();
        }

        public InferenceEnumerator Infer(Guid contextId, LogitRuleCollection? logitRules)
        {
            return new InferenceEnumerator(
                (b) => Predict(contextId, b),
                (t) => Write(contextId, t),
                logitRules
            );
        }

        public virtual async Task<ClientResponse> PostAsync(string host, string url, JsonContent content)
        {
            HttpResponseMessage hr = await HttpClient.PostAsync(host + url, content);

            return new ClientResponse()
            {
                Body = await hr.Content.ReadAsStringAsync(),
                Status = (int)hr.StatusCode
            };
        }

        public virtual async Task<ClientResponse> PostAsync(string host, string url, string content)
        {
            HttpResponseMessage hr = await HttpClient.PostAsync(host + url, new StringContent(content));

            return new ClientResponse()
            {
                Body = await hr.Content.ReadAsStringAsync(),
                Status = (int)hr.StatusCode
            };
        }

        public async Task<ResponseLlamaToken> Predict(Guid contextId, LogitRuleCollection? ruleCollection = null)
        {
            PredictRequest request = new()
            {
                ContextId = contextId
            };

            if (ruleCollection != null)
            {
                request.LogitRules = ruleCollection;
            }

            PredictResponse response;

            response = await PostCollection<PredictResponse>("/Llama/predict", request, contextId);

            return response.Predicted;
        }

        public async Task<IReadOnlyLlamaTokenCollection> Tokenize(Guid contextId, string s)
        {
            if (s.Length == 0)
            {
                return new LlamaTokenCollection();
            }

            TokenizeResponse response = await PostCollection<TokenizeResponse>("/Llama/tokenize", new TokenizeRequest()
            {
                Content = s,
                ContextId = contextId
            }, contextId);

            return new LlamaTokenCollection(response.Tokens.Select(lt => new LlamaToken(lt.Id, lt.Value)));
        }

        public async Task<ContextState> Write(Guid contextId, RequestLlamaToken requestLlamaToken, int startIndex = -1)
        {
            List<RequestLlamaToken> tokens = new()
            {
                requestLlamaToken
            };

            return await Write(contextId, tokens, startIndex);
        }

        public async Task<ContextState> Write(Guid contextId, IEnumerable<RequestLlamaToken> requestLlamaTokens, int startIndex = -1)
        {
            WriteTokenRequest request = new()
            {
                ContextId = contextId,
                Tokens = requestLlamaTokens.ToList(),
                StartIndex = startIndex
            };

            WriteTokenResponse response;

            response = await PostCollection<WriteTokenResponse>("/Llama/write", request, contextId);

            return response.State;
        }

        public async Task<ContextState> Write(Guid contextId, string s, int startIndex = -1)
        {
            IReadOnlyLlamaTokenCollection tokens = await Tokenize(contextId, s);

            return await Write(contextId, tokens.Select(r => new RequestLlamaToken() { TokenId = r.Id }), startIndex);
        }

        protected virtual async Task<ContextState> LoadContext(LlamaContextSettings settings, Guid contextId)
        {
            ContextRequest cr = new()
            {
                ContextId = contextId,
                Settings = settings,
                ModelId = _modelGuid,
                ContextRequestSettings = _contextRequestSettings
            };

            ContextResponse loadResponse = await PostCollection<ContextResponse>("/Llama/context", cr, contextId);

            return loadResponse.State;
        }

        protected async Task<ModelResponse> LoadModel(LlamaModelSettings settings)
        {
            return await PostCollection<ModelResponse>("/Llama/model", new ModelRequest()
            {
                Settings = settings,
                ModelId = _modelGuid
            }, Guid.Empty);
        }

        private async Task<TValue> Get<TValue>(string url, Guid contextId)
        {
            string responseStr = await Request(() => GetAsync(_settings.Host, url), contextId);

            return JsonSerializer.Deserialize<TValue>(responseStr);
        }

        private async Task<TValue> Post<TValue>(string url, object data, Guid contextId)
        {
            string responseStr = await Request(() => PostAsync(_settings.Host, url, JsonContent.Create(data, options: _serializerOptions)), contextId);

            return JsonSerializer.Deserialize<TValue>(responseStr, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }

        private async Task<TValue> PostCollection<TValue>(string url, object data, Guid contextId)
        {
            RequestCollection requestCollection = new ();

            requestCollection.Requests.Add(data);

            byte[] bytes = DataSerializer.Serialize(requestCollection);

            string b64 = Convert.ToBase64String(bytes);

            string responseStr = await Request(() => PostAsync(_settings.Host, "/Llama/Request", b64), contextId);

            byte[] r_bytes = Convert.FromBase64String(responseStr);

            ResponseCollection responseCollection = DataSerializer.Deserialize<ResponseCollection>(r_bytes);

            return (TValue)responseCollection.Responses.Single();
        }

        private async Task Post(string url, object data, Guid contextId)
        {
            await Request(() => PostAsync(_settings.Host, url, JsonContent.Create(data, options: _serializerOptions)), contextId);
        }

        private async Task<string> Request(Func<Task<ClientResponse>> toInvoke, Guid contextId)
        {
            do
            {
                ClientResponse r = await toInvoke.Invoke();

                string responseStr = r.Body;

                if (r.Status == (int)LlamaStatusCodes.NoModelLoaded)
                {
                    await LoadModel(_modelSettings);
                    continue;
                }

                if (r.Status == (int)LlamaStatusCodes.NoContextLoaded)
                {
                    if (contextId == Guid.Empty)
                    {
                        throw new InvalidOperationException("Server requires context load but no context id proviced");
                    }

                    await LoadContext(_contextSettings, contextId);
                    continue;
                }

                if (r.Status >= 400)
                {
                    throw new Exception(responseStr);
                }

                if (r.Status == (int)LlamaStatusCodes.NotReady)
                {
                    await Task.Delay(1000);
                    continue;
                }

                return responseStr;
            } while (true);
        }
    }
}