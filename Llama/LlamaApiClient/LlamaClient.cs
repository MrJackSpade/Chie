using Llama.Data;
using Llama.Data.Enums;
using LlamaApi.Models;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Models.Request;
using System.Net.Http.Json;
using System.Text.Json;

namespace LlamaApiClient
{
    public class LlamaClient
    {
        private readonly LlamaClientSettings _settings;
        private readonly HttpClient _httpClient = new();
        private readonly Guid _modelGuid = Guid.Empty;
        public LlamaClient(LlamaClientSettings settings)
        {
            this._settings = settings;
        }

        private async Task<TValue> Post<TValue>(string url, object data)
        {
            HttpResponseMessage response = await this._httpClient.PostAsync(this._settings.Host + url, JsonContent.Create(data));
            string responseStr = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TValue>(responseStr);
        }

        private async Task Post(string url, object data)
        {
            HttpResponseMessage response = await this._httpClient.PostAsync(this._settings.Host + url, JsonContent.Create(data));
            string responseStr = await response.Content.ReadAsStringAsync();
        }

        private async Task<TValue> Get<TValue>(string url)
        {
            HttpResponseMessage response = await this._httpClient.GetAsync(this._settings.Host + url);
            string responseStr = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TValue>(responseStr);
        }

        private async Task WaitForResponse(string url, object data)
        {
            Job j = await this.Post<Job>(url, data);

            do
            {
                JobResponse jobResponse = await this.Get<JobResponse>($"/job/{j.Id}");

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
                JobResponse<TOut> jobResponse = await this.Get<JobResponse<TOut>>($"/job/{j.Id}");

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

        public async Task<ModelResponse> LoadModel(LlamaModelSettings settings)
        {
            return await this.WaitForResponse<ModelResponse>("/Llama/model", new ModelRequest()
            {
                Settings = settings,
                ModelId = this._modelGuid
            });
        }

        public async Task<ContextState> LoadContext(LlamaContextSettings settings)
        {
            ContextResponse loadResponse = await this.WaitForResponse<ContextResponse>("/Llama/context", new ContextRequest()
            {
                Settings = settings,
                ModelId = this._modelGuid
            });

            return loadResponse.State;
        }

        public async void DisposeContext(Guid contextId)
        {
            await this.Post("/Llama/context/dispose", new ContextDisposeRequest()
            {
                ContextId = contextId
            });
        }

        public async Task<InferenceEnumerator> Infer(Guid contextId)
        {
            return new InferenceEnumerator(
                () => this.Predict(contextId),
                (t) => this.Write(contextId,t)
            );
        }

        public async Task<ResponseLlamaToken> Predict(Guid contextId)
        {
            PredictResponse response = await this.WaitForResponse<PredictResponse>("/Llama/predict", new PredictRequest()
            {
                ContextId = contextId
            });

            return response.Predicted;
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

        public async Task<ContextState> Write(Guid contextId, string s, LlamaTokenType llamaTokenType = LlamaTokenType.Input, int startIndex = -1)
        {
            int[] tokens = await this.Tokenize(contextId, s);

            WriteTokenRequest request = new()
            {
                ContextId = contextId,
                StartIndex = startIndex,
            };

            foreach(int token in tokens)
            {
                request.Tokens.Add(new RequestLlamaToken()
                {
                    TokenId = token,
                    TokenType = llamaTokenType
                });
            }

            WriteTokenResponse response = await this.WaitForResponse<WriteTokenResponse>("/Llama/write", request);

            return response.State;
        }

        public async Task<int[]> Tokenize(Guid contextId, string s)
        {
            if(s.Length == 0)
            {
                return Array.Empty<int>(); 
            }

            TokenizeResponse response = await this.WaitForResponse<TokenizeResponse>("/Llama/tokenize", new TokenizeRequest()
            {
                Content = s,
                ContextId = contextId
            });

            return response.Tokens;
        }

        public async Task Eval(Guid contextId)
        {
            await this.WaitForResponse("/Llama/eval", new EvaluateRequest()
            {
                ContextId = contextId
            });
        }
    }
}
