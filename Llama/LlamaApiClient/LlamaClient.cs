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

        private readonly LogitRuleCollection _logitRules = new();

        private readonly Guid _modelGuid = Guid.Empty;

        private readonly LlamaModelSettings _modelSettings;

        private readonly JsonSerializerOptions _serializerOptions;

        private readonly LlamaClientSettings _settings;

        private readonly List<object> _queuedRequests;

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
            _queuedRequests = new List<object>();
        }

        protected HttpClient HttpClient { get; }

        public async Task DisposeContext()
        {
            await this.Post("/Llama/context/dispose", new ContextDisposeRequest()
            {
                ContextId = _settings.LlamaContextId
            });
        }

        public async Task Eval()
        {
            EvaluationResponse response = await this.QueueAndFlush<EvaluationResponse>(new EvaluateRequest()
            {
                ContextId = _settings.LlamaContextId
            });

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

        public async Task<float[]> GetLogits()
        {
            GetLogitsRequest request = new()
            {
                ContextId = _settings.LlamaContextId
            };

            GetLogitsResponse response = await this.QueueAndFlush<GetLogitsResponse>(request);

            return response.GetValue().ToArray();
        }

        public InferenceEnumerator Infer()
        {
            InferenceEnumerator toReturn = new(
                this._modelSettings.SpecialTokens,
                this.Predict,
                (t) => this.Write(t),
                _logitRules
            );

            _logitRules.Remove(LogitRuleLifetime.Inferrence);

            return toReturn;
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

        public async Task<ResponseLlamaToken> Predict(LogitRuleCollection? ruleCollection = null)
        {
            PredictRequest request = new()
            {
                ContextId = _settings.LlamaContextId
            };

            if (ruleCollection != null)
            {
                request.LogitRules = ruleCollection;
            }

            PredictResponse response;

            response = await this.QueueAndFlush<PredictResponse>(request);

            return response.Predicted;
        }

        public async Task<IReadOnlyLlamaTokenCollection> Tokenize(string s)
        {
            if (s.Length == 0)
            {
                return new LlamaTokenCollection();
            }

            TokenizeResponse response = await this.QueueAndFlush<TokenizeResponse>(new TokenizeRequest()
            {
                Content = s,
                ContextId = _settings.LlamaContextId
            });

            return new LlamaTokenCollection(response.Tokens.Select(lt => new LlamaToken(lt.Id, lt.Value)));
        }

        public async Task Write(RequestLlamaToken requestLlamaToken, int startIndex = -1)
        {
            List<RequestLlamaToken> tokens = new()
            {
                requestLlamaToken
            };

            await this.Write(tokens, startIndex);
        }

        public async Task Write(IEnumerable<RequestLlamaToken> requestLlamaTokens, int startIndex = -1)
        {
            WriteTokenRequest request = new()
            {
                ContextId = _settings.LlamaContextId,
                Tokens = requestLlamaTokens.ToList(),
                StartIndex = startIndex
            };

            if (startIndex != -1)
            {
                await this.QueueAndFlush<WriteTokenResponse>(request);
            }
            else
            {
                _queuedRequests.Add(request);
            }
        }

        public async Task Write(string s, int startIndex = -1)
        {
            IReadOnlyLlamaTokenCollection tokens = await this.Tokenize(s);

            await this.Write(tokens.Select(r => new RequestLlamaToken() { TokenId = r.Id }), startIndex);
        }

        protected virtual async Task<ContextState> LoadContext(LlamaContextSettings settings)
        {
            ContextRequest cr = new()
            {
                ContextId = _settings.LlamaContextId,
                Settings = settings,
                ModelId = _modelGuid,
                ContextRequestSettings = _contextRequestSettings
            };

            ContextResponse loadResponse = await this.QueueAndFlush<ContextResponse>(cr);

            return loadResponse.State;
        }

        protected async Task<ModelResponse> LoadModel(LlamaModelSettings settings)
        {
            return await this.QueueAndFlush<ModelResponse>(new ModelRequest()
            {
                Settings = settings,
                ModelId = _modelGuid
            });
        }

        private async Task<TValue> FlushQueue<TValue>()
        {
            RequestCollection requestCollection = new();

            foreach (object data in _queuedRequests)
            {
                requestCollection.Requests.Add(data);
            }

            _queuedRequests.Clear();

            byte[] bytes = DataSerializer.Serialize(requestCollection);

            string b64 = Convert.ToBase64String(bytes);

            string responseStr = await this.Request(() => this.PostAsync(_settings.Host, "/Llama/Request", b64));

            byte[] r_bytes = Convert.FromBase64String(responseStr);

            ResponseCollection responseCollection = DataSerializer.Deserialize<ResponseCollection>(r_bytes);

            return (TValue)responseCollection.Responses.Last();
        }

        private async Task Post(string url, object data)
        {
            await this.Request(() => this.PostAsync(_settings.Host, url, JsonContent.Create(data, options: _serializerOptions)));
        }

        private async Task<TValue> QueueAndFlush<TValue>(object request)
        {
            _queuedRequests.Add(request);
            return await this.FlushQueue<TValue>();
        }

        private async Task<string> Request(Func<Task<ClientResponse>> toInvoke)
        {
            do
            {
                ClientResponse r = await toInvoke.Invoke();

                string responseStr = r.Body;

                if (r.Status == (int)LlamaStatusCodes.NoModelLoaded)
                {
                    await this.LoadModel(_modelSettings);
                    continue;
                }

                if (r.Status == (int)LlamaStatusCodes.NoContextLoaded)
                {
                    await this.LoadContext(_contextSettings);
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