using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Models;
using LlamaApi.Models;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Models.Request;
using LlamaApi.Shared.Models.Response;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace LlamaApiClient
{
    public class LlamaClient
    {
        private readonly HttpClient _httpClient = new();

        private readonly Guid _modelGuid = Guid.Empty;

        private readonly LlamaClientSettings _settings;

        public LlamaClient(LlamaClientSettings settings)
        {
            this._settings = settings;
        }

        public async Task DisposeContext(Guid contextId)
        {
            await this.Post("/Llama/context/dispose", new ContextDisposeRequest()
            {
                ContextId = contextId
            });
        }

        public async Task Eval(Guid contextId)
        {
            EvaluationResponse response = await this.WaitForResponse<EvaluationResponse>("/Llama/eval", new EvaluateRequest()
            {
                ContextId = contextId
            });

            Debug.WriteLine("Evaluated: " + response.Evaluated);
        }

        public InferenceEnumerator Infer(Guid contextId)
        {
            return new InferenceEnumerator(
                (b) => this.Predict(contextId, b),
                (t) => this.Write(contextId, t)
            );
        }

        public virtual async Task<ContextState> LoadContext(LlamaContextSettings settings, Action<ContextRequest> settingsAction)
        {
            ContextRequest cr = new()
            {
                Settings = settings,
                ModelId = this._modelGuid
            };

            settingsAction.Invoke(cr);

            ContextResponse loadResponse = await this.WaitForResponse<ContextResponse>("/Llama/context", cr);

            return loadResponse.State;
        }

        public async Task<ModelResponse> LoadModel(LlamaModelSettings settings)
        {
            return await this.WaitForResponse<ModelResponse>("/Llama/model", new ModelRequest()
            {
                Settings = settings,
                ModelId = this._modelGuid
            });
        }

        public async Task<ResponseLlamaToken> Predict(Guid contextId, Dictionary<int, float>? bias = null)
        {
            PredictRequest request = new()
            {
                ContextId = contextId
            };

            if (bias != null)
            {
                request.LogitBias = bias;
            }

            PredictResponse response = await this.WaitForResponse<PredictResponse>("/Llama/predict", request);

            return response.Predicted;
        }

        public async Task<LlamaTokenCollection> Tokenize(Guid contextId, string s)
        {
            if (s.Length == 0)
            {
                return new LlamaTokenCollection();
            }

            TokenizeResponse response = await this.WaitForResponse<TokenizeResponse>("/Llama/tokenize", new TokenizeRequest()
            {
                Content = s,
                ContextId = contextId
            });

            return new LlamaTokenCollection(response.Tokens);
        }

        public async Task<ContextState> Write(Guid contextId, RequestLlamaToken requestLlamaToken, int startIndex = -1)
        {
            WriteTokenRequest request = new()
            {
                ContextId = contextId,
                Tokens = new List<RequestLlamaToken> { requestLlamaToken },
                StartIndex = startIndex
            };

            WriteTokenResponse response = await this.WaitForResponse<WriteTokenResponse>("/Llama/write", request);

            return response.State;
        }

        public async Task<ContextState> Write(Guid contextId, IEnumerable<RequestLlamaToken> requestLlamaTokens, int startIndex = -1)
        {
            WriteTokenRequest request = new()
            {
                ContextId = contextId,
                Tokens = requestLlamaTokens.ToList(),
                StartIndex = startIndex
            };

            WriteTokenResponse response = await this.WaitForResponse<WriteTokenResponse>("/Llama/write", request);

            return response.State;
        }

        public async Task<ContextState> Write(Guid contextId, string s, int startIndex = -1)
        {
            LlamaTokenCollection tokens = await this.Tokenize(contextId, s);

            WriteTokenRequest request = new()
            {
                ContextId = contextId,
                StartIndex = startIndex,
            };

            foreach (LlamaToken token in tokens)
            {
                request.Tokens.Add(new RequestLlamaToken()
                {
                    TokenId = token.Id
                });
            }

            WriteTokenResponse response = await this.WaitForResponse<WriteTokenResponse>("/Llama/write", request);

            return response.State;
        }

        private async Task<TValue> Get<TValue>(string url)
        {
            string responseStr = await this.Request(() => this._httpClient.GetAsync(this._settings.Host + url));

            return JsonSerializer.Deserialize<TValue>(responseStr);
        }

        private async Task<TValue> Post<TValue>(string url, object data)
        {
            string responseStr = await this.Request(() => this._httpClient.PostAsync(this._settings.Host + url, JsonContent.Create(data)));

            return JsonSerializer.Deserialize<TValue>(responseStr);
        }

        private async Task Post(string url, object data) => await this.Request(() => this._httpClient.PostAsync(this._settings.Host + url, JsonContent.Create(data)));

        private async Task<string> Request(Func<Task<HttpResponseMessage>> toInvoke)
        {
            do
            {
                HttpResponseMessage r = await toInvoke.Invoke();

                string responseStr = await r.Content.ReadAsStringAsync();

                if ((int)r.StatusCode >= 400)
                {
                    throw new Exception(responseStr);
                }

                if (r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(1000);
                    continue;
                }

                return responseStr;
            } while (true);
        }

        private async Task WaitForResponse(string url, object data)
        {
            Job j = await this.Post<Job>(url, data);

            do
            {
                JobResponse jobResponse = await this.Get<JobResponse>($"/Llama/job/{j.Id}");

                if (jobResponse.State == JobState.Success)
                {
                    return;
                }

                if (jobResponse.State == JobState.Failure)
                {
                    throw new Exception();
                }

                await Task.Delay(this._settings.PollingFrequencyMs);
            } while (true);
        }

        private async Task<TOut> WaitForResponse<TOut>(string url, object data)
        {
            Job j = await this.Post<Job>(url, data);

            do
            {
                JobResponse<TOut> jobResponse = await this.Get<JobResponse<TOut>>($"/Llama/job/{j.Id}");

                if (jobResponse.State == JobState.Success)
                {
                    return jobResponse.Result;
                }

                if (jobResponse.State == JobState.Failure)
                {
                    throw new Exception();
                }

                await Task.Delay(this._settings.PollingFrequencyMs);
            } while (true);
        }
    }
}