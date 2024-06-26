using Llama.Core;
using Llama.Core.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;
using Llama.Extensions;
using Llama.Native;
using LlamaApi.Exceptions;
using LlamaApi.Extensions;
using LlamaApi.Models;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Models;
using LlamaApi.Shared.Models.Request;
using LlamaApi.Shared.Models.Response;
using LlamaApi.Shared.Serializers;
using Microsoft.AspNetCore.Mvc;

namespace LlamaApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public partial class LlamaController : ControllerBase
    {
        private readonly IExecutionScheduler _contextExecutionScheduler;

        private readonly LoadedModel _loadedModel;

        public LlamaController(IExecutionScheduler contextExecutionScheduler, LoadedModel loadedModel)
        {
            _contextExecutionScheduler = contextExecutionScheduler;
            _loadedModel = loadedModel;
        }

        [HttpPost("context")]
        public ContextResponse Context(ContextRequest request)
        {
            _loadedModel.Lock();

            Guid? contextId = request?.ContextId;

            try
            {
                if (_loadedModel?.Id != request.ModelId)
                {
                    return this.StatusCode<ContextResponse>(LlamaStatusCodes.NoModelLoaded);
                }

                if (contextId.HasValue && _loadedModel.Evaluator.ContainsKey(contextId.Value))
                {
                    ContextInstance contextEvaluator = _loadedModel.GetContext(contextId.Value);

                    return new ContextResponse()
                    {
                        State = new ContextState()
                        {
                            Id = contextId ?? Guid.NewGuid(),
                            AvailableBuffer = contextEvaluator.Context.AvailableBuffer,
                            IsLoaded = true,
                            Size = contextEvaluator.Context.Size
                        }
                    };
                }

                SafeLlamaContextHandle safeLlamaContextHandle = NativeApi.LoadContext(_loadedModel.Instance.Handle, request.Settings);

                LlamaContextWrapper wrapper = new(_contextExecutionScheduler,
                                                   safeLlamaContextHandle,
                                                   _loadedModel.Instance.Handle,
                                                   request.Settings,
                                                   this.GetSimpleSamplers(request),
                                                   this.GetFinalSampler(request)
                                                   );

                ContextInstance evaluator = new(wrapper);

                ContextResponse response = new()
                {
                    State = new ContextState()
                    {
                        Id = contextId ?? Guid.NewGuid(),
                        AvailableBuffer = wrapper.AvailableBuffer,
                        IsLoaded = true,
                        Size = wrapper.Size
                    }
                };

                _loadedModel.Evaluator.Add(response.State.Id, evaluator);

                //for (int i = 0; i < 1; i++)
                //{
                //    WriteTokenRequest writeTokenRequest = new() { ContextId = response.State.Id, Priority = ExecutionPriority.Immediate, StartIndex = 0, Tokens = new List<RequestLlamaToken>(), WriteTokenType = WriteTokenType.Overwrite };

                //    Random r = new();

                //    for (int ii = 0; ii < request.Settings.ContextSize - 2000; ii++)
                //    {
                //        writeTokenRequest.Tokens.Add(new RequestLlamaToken() { TokenId = r.Next(1, 10000) });
                //    }

                //    this.Write(writeTokenRequest);

                //    this.Eval(new EvaluateRequest() { ContextId = response.State.Id, Priority = ExecutionPriority.Immediate });
                //}

                return response;
            }
            finally
            {
                _loadedModel.Unlock();
            }
        }

        [HttpPost("context/dispose")]
        public IActionResult ContextDispose(ContextDisposeRequest request)
        {
            ContextInstance context = _loadedModel.GetContext(request.ContextId);

            context.Dispose();

            return this.Ok();
        }

        [HttpPost("eval")]
        public EvaluationResponse Eval(EvaluateRequest request)
        {
            if (!this.TryLoadContext(request.ContextId, out ContextInstance context, out EvaluationResponse response))
            {
                return response;
            }

            context.Evaluate(request.Priority);

            return new EvaluationResponse()
            {
                AvailableBuffer = context.Context.AvailableBuffer,
                Id = request.ContextId,
                IsLoaded = true,
                Evaluated = 0
            };
        }

        [HttpPost("evaluated")]
        public ContextSnapshotResponse Evaluated(ContextSnapshotRequest request)
        {
            if (!this.TryLoadContext(request.ContextId, out ContextInstance context, out ContextSnapshotResponse response))
            {
                return response;
            }

            return new ContextSnapshotResponse()
            {
                Tokens = context.Context.Evaluated.Select(t => new ResponseLlamaToken(t)).ToArray()
            };
        }

        [HttpPost("getlogits")]
        public GetLogitsResponse GetLogits(GetLogitsRequest request)
        {
            if (!this.TryLoadContext(request.ContextId, out ContextInstance context, out GetLogitsResponse response))
            {
                return response;
            }

            Span<float> logits = context.Context.GetLogits();

            response = new();

            response.SetValue(logits);

            return response;
        }

        [HttpPost("model")]
        public ModelResponse Model(ModelRequest request)
        {
            _loadedModel.Lock();

            try
            {
                if (_loadedModel.Instance != null)
                {
                    if (_loadedModel.Settings.Model == request.Settings.Model)
                    {
                        return new ModelResponse()
                        {
                            Id = _loadedModel.Id
                        };
                    }

                    throw new DuplicateModelLoadException();
                }

                _loadedModel.Instance = NativeApi.LoadModel(request.Settings);

                _loadedModel.Settings = request.Settings;

                _loadedModel.Id = request.ModelId;

                return new ModelResponse()
                {
                    Id = _loadedModel.Id
                };
            }
            finally
            {
                _loadedModel.Unlock();
            }
        }

        [HttpPost("predict")]
        public PredictResponse Predict(PredictRequest request)
        {
            if (!this.TryLoadContext(request.ContextId, out ContextInstance context, out PredictResponse response))
            {
                return response;
            }

            LlamaToken predicted = context.Predict(request.Priority, request.LogitRules);

            return new PredictResponse()
            {
                Predicted = new ResponseLlamaToken(predicted)
            };
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestCollection()
        {
            // Read the content as a string (assuming it's sent as plain text)
            string contentAsBase64 = new StreamReader(HttpContext.Request.BodyReader.AsStream()).ReadToEnd();

            try
            {
                // Convert the base64 string to byte array
                byte[] fileBytes = Convert.FromBase64String(contentAsBase64);

                RequestCollection requests = DataSerializer.Deserialize<RequestCollection>(fileBytes);

                ResponseCollection responses = new();

                foreach (object o in requests.Requests)
                {
                    object r = null;
                    bool found = false;

                    if (o is ContextDisposeRequest cdr)
                    {
                        throw new NotImplementedException();
                    }

                    if (o is ContextRequest cr)
                    {
                        r = this.Context(cr);
                        found = true;
                    }

                    if (o is ContextSnapshotRequest csr)
                    {
                        r = this.Evaluated(csr);
                        found = true;
                    }

                    if (o is EvaluateRequest er)
                    {
                        r = this.Eval(er);
                        found = true;
                    }

                    if (o is GetLogitsRequest glr)
                    {
                        r = this.GetLogits(glr);
                        found = true;
                    }

                    if (o is ModelRequest mr)
                    {
                        r = this.Model(mr);
                        found = true;
                    }

                    if (o is PredictRequest pr)
                    {
                        r = this.Predict(pr);
                        found = true;
                    }

                    if (o is TokenizeRequest tr)
                    {
                        r = this.Tokenize(tr);
                        found = true;
                    }

                    if (o is WriteTokenRequest wtr)
                    {
                        r = this.Write(wtr);
                        found = true;
                    }

                    if (!found)
                    {
                        throw new NotImplementedException();
                    }

                    responses.Responses.Add(r);
                }

                byte[] responseData = DataSerializer.Serialize(responses);

                string responseStr = Convert.ToBase64String(responseData);

                return this.Content(responseStr);
            }
            catch (FormatException ex)
            {
                // Handle the exception if the string is not a valid base64 string
                return this.BadRequest("Invalid Base64 string.");
            }
        }

        [HttpGet("/")]
        public IActionResult State()
        {
            return this.Content("OK");
        }

        public T StatusCode<T>(LlamaStatusCodes status)
        {
            T result = default;

            HttpContext.Response.StatusCode = (int)status;

            return result;
        }

        [HttpPost("tokenize")]
        public TokenizeResponse Tokenize(TokenizeRequest request)
        {
            if (_loadedModel?.Instance is null)
            {
                return this.StatusCode<TokenizeResponse>(LlamaStatusCodes.NoModelLoaded);
            }

            List<int> tokens = NativeApi.LlamaTokenize(_loadedModel.Instance.Handle, request.Content!, false);

            List<LlamaToken> toReturn = new();

            foreach (int token in tokens)
            {
                toReturn.Add(new LlamaToken(token, NativeApi.TokenToPiece(_loadedModel.Instance.Handle, token)));
            }

            return new TokenizeResponse()
            {
                Tokens = toReturn.Select(t => new ResponseLlamaToken(t)).ToArray()
            };
        }

        public bool TryLoadContext<T>(Guid guid, out ContextInstance context, out T response)
        {
            response = default;

            bool success = true;

            if (_loadedModel?.Instance is null)
            {
                response = this.StatusCode<T>(LlamaStatusCodes.NoModelLoaded);
                success = false;
            }

            if (!_loadedModel.TryGetContext(guid, out context))
            {
                response = this.StatusCode<T>(LlamaStatusCodes.NoContextLoaded);
                success = false;
            }

            return success;
        }

        [HttpPost("write")]
        public WriteTokenResponse Write(WriteTokenRequest request)
        {
            if (request.WriteTokenType == WriteTokenType.Insert)
            {
                throw new NotImplementedException();
            }

            if (!this.TryLoadContext(request.ContextId, out ContextInstance context, out WriteTokenResponse response))
            {
                return response;
            }

            LlamaTokenCollection toWrite = new();

            foreach (RequestLlamaToken token in request.Tokens)
            {
                string value = NativeApi.TokenToPiece(_loadedModel.Instance.Handle, token.TokenId);
                toWrite.Append(new LlamaToken(token.TokenId, value));
            }

            if (request.StartIndex >= 0)
            {
                context.Context.SetBufferPointer((uint)request.StartIndex);
            }

            context.Write(toWrite);

            return new WriteTokenResponse()
            {
                State = new ContextState()
                {
                    Id = Guid.NewGuid(),
                    AvailableBuffer = context.Context.AvailableBuffer,
                    IsLoaded = true,
                    Size = context.Context.Size
                }
            };
        }

        private ITokenSelector GetFinalSampler(ContextRequest contextRequest)
        {
            SamplerSetting samplerSetting = contextRequest.SamplerSettings.Last();

            return samplerSetting.InstantiateSelector();
        }

        private IEnumerable<ISimpleSampler> GetSimpleSamplers(ContextRequest contextRequest)
        {
            for (int i = 0; i < contextRequest.SamplerSettings.Length - 1; i++)
            {
                SamplerSetting settings = contextRequest.SamplerSettings[i];

                yield return settings.InstantiateSimple();
            }
        }
    }
}