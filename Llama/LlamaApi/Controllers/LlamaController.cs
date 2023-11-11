using Llama.Core;
using Llama.Core.Interfaces;
using Llama.Core.Samplers.FrequencyAndPresence;
using Llama.Core.Samplers.Mirostat;
using Llama.Core.Samplers.Repetition;
using Llama.Core.Samplers.Temperature;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;
using Llama.Extensions;
using Llama.Native;
using LlamaApi.Exceptions;
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
            this._contextExecutionScheduler = contextExecutionScheduler;
            this._loadedModel = loadedModel;
        }

        [HttpPost("context")]
        public ContextResponse Context(ContextRequest request)
        {
            this._loadedModel.Lock();

            Guid? contextId = request?.ContextId;

            try
            {
                if (this._loadedModel?.Id != request.ModelId)
                {
                    return StatusCode<ContextResponse>(LlamaStatusCodes.NoModelLoaded);
                }

                if (contextId.HasValue && this._loadedModel.Evaluator.ContainsKey(contextId.Value))
                {
                    ContextInstance contextEvaluator = this._loadedModel.GetContext(contextId.Value);

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

                SafeLlamaContextHandle safeLlamaContextHandle = NativeApi.LoadContext(this._loadedModel.Instance.Handle, request.Settings);

                LlamaContextWrapper wrapper = new(this._contextExecutionScheduler,
                                                   safeLlamaContextHandle,
                                                   this._loadedModel.Instance.Handle,
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

                this._loadedModel.Evaluator.Add(response.State.Id, evaluator);

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
                this._loadedModel.Unlock();
            }
        }

        [HttpPost("context/dispose")]
        public IActionResult ContextDispose(ContextDisposeRequest request)
        {
            ContextInstance context = this._loadedModel.GetContext(request.ContextId);

            context.Dispose();

            return this.Ok();
        }

        [HttpPost("eval")]
        public EvaluationResponse Eval(EvaluateRequest request)
        {
            if (!TryLoadContext(request.ContextId, out ContextInstance context, out EvaluationResponse response))
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
            if (!TryLoadContext(request.ContextId, out ContextInstance context, out ContextSnapshotResponse response))
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
            if (!TryLoadContext(request.ContextId, out ContextInstance context, out GetLogitsResponse response))
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
            this._loadedModel.Lock();

            try
            {
                if (this._loadedModel.Instance != null)
                {
                    if (this._loadedModel.Settings.Model == request.Settings.Model)
                    {
                        return new ModelResponse()
                        {
                            Id = this._loadedModel.Id
                        };
                    }

                    throw new DuplicateModelLoadException();
                }

                this._loadedModel.Instance = NativeApi.LoadModel(request.Settings);

                this._loadedModel.Settings = request.Settings;

                this._loadedModel.Id = request.ModelId;

                return new ModelResponse()
                {
                    Id = this._loadedModel.Id
                };
            }
            finally
            {
                this._loadedModel.Unlock();
            }
        }

        [HttpPost("predict")]
        public PredictResponse Predict(PredictRequest request)
        {
            if (!TryLoadContext(request.ContextId, out ContextInstance context, out PredictResponse response))
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
                        r = Context(cr);
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

                return Content(responseStr);
            }
            catch (FormatException ex)
            {
                // Handle the exception if the string is not a valid base64 string
                return BadRequest("Invalid Base64 string.");
            }
        }

        [HttpGet("/")]
        public IActionResult State() => this.Content("OK");

        public T StatusCode<T>(LlamaStatusCodes status)
        {
            T result = default;

            this.HttpContext.Response.StatusCode = (int)status;

            return result;
        }

        [HttpPost("tokenize")]
        public TokenizeResponse Tokenize(TokenizeRequest request)
        {
            if (this._loadedModel?.Instance is null)
            {
                return StatusCode<TokenizeResponse>(LlamaStatusCodes.NoModelLoaded);
            }

            List<int> tokens = NativeApi.LlamaTokenize(this._loadedModel.Instance.Handle, request.Content!, false);

            List<LlamaToken> toReturn = new();

            foreach (int token in tokens)
            {
                if (token == 0)
                {
                    throw new InvalidOperationException("Null token detected in tokenize response");
                }

                toReturn.Add(new LlamaToken(token, NativeApi.TokenToPiece(this._loadedModel.Instance.Handle, token)));
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

            if (this._loadedModel?.Instance is null)
            {
                response = StatusCode<T>(LlamaStatusCodes.NoModelLoaded);
                success = false;
            }

            if (!this._loadedModel.TryGetContext(guid, out context))
            {
                response = StatusCode<T>(LlamaStatusCodes.NoContextLoaded);
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

            if (!TryLoadContext(request.ContextId, out ContextInstance context, out WriteTokenResponse response))
            {
                return response;
            }

            LlamaTokenCollection toWrite = new();

            foreach (RequestLlamaToken token in request.Tokens)
            {
                string value = NativeApi.TokenToPiece(this._loadedModel.Instance.Handle, token.TokenId);
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
            ContextRequestSettings request = contextRequest.ContextRequestSettings;

            if (request.MirostatTempSamplerSettings != null)
            {
                return new MirostatTempSampler(request.MirostatTempSamplerSettings);
            }

            if (request.MirostatSamplerSettings != null)
            {
                return request.MirostatSamplerSettings.MirostatType switch
                {
                    MirostatType.One => new MirostatOneSampler(request.MirostatSamplerSettings),
                    MirostatType.Two => new MirostatTwoSampler(request.MirostatSamplerSettings),
                    _ => throw new NotImplementedException(),
                };
            }

            if (request.TemperatureSamplerSettings != null)
            {
                if (request.TemperatureSamplerSettings.Temperature < 0)
                {
                    return new GreedySampler();
                }
                else
                {
                    return new TemperatureSampler(request.TemperatureSamplerSettings);
                }
            }

            throw new NotImplementedException();
        }

        private IEnumerable<ISimpleSampler> GetSimpleSamplers(ContextRequest contextRequest)
        {
            ContextRequestSettings request = contextRequest.ContextRequestSettings;

            if (request.RepetitionSamplerSettings != null)
            {
                yield return new RepetitionSampler(request.RepetitionSamplerSettings);
            }

            if (request.ComplexPresencePenaltySettings != null)
            {
                yield return new ComplexPresenceSampler(request.ComplexPresencePenaltySettings);
            }
        }
    }
}