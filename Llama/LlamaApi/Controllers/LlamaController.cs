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
using Llama.Native;
using LlamaApi.Exceptions;
using LlamaApi.Interfaces;
using LlamaApi.Models;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace LlamaApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LlamaController : ControllerBase
    {
        private readonly IExecutionScheduler _contextExecutionScheduler;

        private readonly IContextService _contextService;

        private readonly IJobService _jobService;

        private readonly LoadedModel _loadedModel;

        public LlamaController(IExecutionScheduler contextExecutionScheduler, LoadedModel loadedModel, IJobService jobService, IContextService contextService)
        {
            this._contextExecutionScheduler = contextExecutionScheduler;
            this._loadedModel = loadedModel;
            this._jobService = jobService;
            this._contextService = contextService;
        }

        [HttpPost("buffer")]
        public Job Buffer(ContextSnapshotRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

                return new ContextSnapshotResponse()
                {
                    Tokens = context.Context.Buffer.Select(t => new ResponseLlamaToken(t)).ToArray()
                };
            }, ExecutionPriority.Immediate);
        }

        [HttpPost("context")]
        public Job Context(ContextRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                this._loadedModel.Lock();

                try
                {
                    if (this._loadedModel?.Id != request.ModelId)
                    {
                        throw new ModelNotLoadedException();
                    }

                    if (request.ContextId.HasValue && this._loadedModel.Evaluator.ContainsKey(request.ContextId.Value))
                    {
                        ContextEvaluator contextEvaluator = this._loadedModel.GetContext(request.ContextId.Value);

                        return new ContextResponse()
                        {
                            State = new ContextState()
                            {
                                Id = request.ContextId ?? Guid.NewGuid(),
                                AvailableBuffer = contextEvaluator.Context.AvailableBuffer,
                                IsLoaded = true,
                                Size = contextEvaluator.Context.Size
                            }
                        };
                    }

                    SafeLlamaContextHandle safeLlamaContextHandle = NativeApi.LoadContext(this._loadedModel.Instance.Handle, this._loadedModel.Settings, request.Settings);

                    LlamaContextWrapper wrapper = new(this._contextExecutionScheduler,
                                                       safeLlamaContextHandle,
                                                       request.Settings,
                                                       this.GetSimpleSamplers(request),
                                                       this.GetFinalSampler(request)
                                                       );

                    ContextEvaluator evaluator = new(wrapper);

                    ContextResponse response = new()
                    {
                        State = new ContextState()
                        {
                            Id = request.ContextId ?? Guid.NewGuid(),
                            AvailableBuffer = wrapper.AvailableBuffer,
                            IsLoaded = true,
                            Size = wrapper.Size
                        }
                    };

                    this._loadedModel.Evaluator.Add(response.State.Id, evaluator);

                    return response;
                }
                finally
                {
                    this._loadedModel.Unlock();
                }
            }, request.Priority);
        }

        [HttpPost("context/dispose")]
        public IActionResult ContextDispose(ContextDisposeRequest request)
        {
            ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

            context.Dispose();

            return this.Ok();
        }

        [HttpPost("eval")]
        public Job Eval(EvaluateRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

                context.Evaluate(request.Priority);

                return new ContextState()
                {
                    Size = context.Context.Size,
                    AvailableBuffer = context.Context.AvailableBuffer,
                    Id = request.ContextId,
                    IsLoaded = true
                };
            }, request.Priority);
        }

        [HttpPost("evaluated")]
        public Job Evaluated(ContextSnapshotRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

                return new ContextSnapshotResponse()
                {
                    Tokens = context.Context.Evaluated.Select(t => new ResponseLlamaToken(t)).ToArray()
                };
            }, ExecutionPriority.Immediate);
        }

        [HttpGet("job/{id}")]
        public JobResponse? Job(long id)
        {
            if (id == 0)
            {
                return null;
            }

            Job? j = this._jobService.Get(id);

            if (j is null)
            {
                return null;
            }

            JsonNode result = null;

            if (j.State == JobState.Success && !string.IsNullOrWhiteSpace(j.Result))
            {
                result = JsonNode.Parse(j.Result);
            }

            return new JobResponse()
            {
                State = j.State,
                Result = result,
                Id = id
            };
        }

        [HttpPost("model")]
        public Job Model(ModelRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                this._loadedModel.Lock();

                try
                {
                    if (this._loadedModel.Instance != null)
                    {
                        if (this._loadedModel.Id == request.ModelId && this._loadedModel.Settings.Model == request.Settings.Model)
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

                    this._loadedModel.Id = request.ModelId ?? Guid.NewGuid();

                    return new ModelResponse()
                    {
                        Id = this._loadedModel.Id
                    };
                }
                finally
                {
                    this._loadedModel.Unlock();
                }
            }, ExecutionPriority.Immediate);
        }

        [HttpPost("predict")]
        public Job Predict(PredictRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

                LlamaToken predicted = context.Predict(request.Priority, request.LogitBias);

                return new PredictResponse()
                {
                    Predicted = new ResponseLlamaToken(predicted)
                };
            }, request.Priority);
        }

        [HttpGet("/")]
        public IActionResult State() => this.Content("OK");

        [HttpPost("tokenize")]
        public Job Tokenize(TokenizeRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

                List<int> tokens = NativeApi.LlamaTokenize(context.Context.Handle, request.Content!, false, System.Text.Encoding.UTF8);

                return new TokenizeResponse()
                {
                    Tokens = tokens.ToArray()
                };
            }, request.Priority);
        }

        [HttpPost("write")]
        public Job Write(WriteTokenRequest request)
        {
            return this._jobService.Enqueue(() =>
            {
                if (request.WriteTokenType == WriteTokenType.Insert)
                {
                    throw new NotImplementedException();
                }

                ContextEvaluator context = this._loadedModel.GetContext(request.ContextId);

                LlamaTokenCollection toWrite = new();

                foreach (RequestLlamaToken token in request.Tokens)
                {
                    string value = NativeApi.TokenToStr(context.Context.Handle, token.TokenId);
                    toWrite.Append(new LlamaToken(token.TokenId, value, token.TokenType));
                }

                if (request.StartIndex >= 0)
                {
                    context.Context.SetBufferPointer(request.StartIndex);
                }

                context.Write(toWrite);

                WriteTokenResponse response = new()
                {
                    State = new ContextState()
                    {
                        Id = Guid.NewGuid(),
                        AvailableBuffer = context.Context.AvailableBuffer,
                        IsLoaded = true,
                        Size = context.Context.Size
                    }
                };

                return response;
            }, request.Priority);
        }

        private ITokenSelector GetFinalSampler(ContextRequest request)
        {
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

            if (request.MirostatSamplerSettings != null)
            {
                return request.MirostatSamplerSettings.MirostatType switch
                {
                    MirostatType.One => new MirostatOneSampler(request.MirostatSamplerSettings),
                    MirostatType.Two => new MirostatTwoSampler(request.MirostatSamplerSettings),
                    _ => throw new NotImplementedException(),
                };
            }

            throw new NotImplementedException();
        }

        private IEnumerable<ISimpleSampler> GetSimpleSamplers(ContextRequest request)
        {
            if (request.RepetitionSamplerSettings != null)
            {
                yield return new RepetitionSampler(request.RepetitionSamplerSettings);
            }

            if (request.FrequencyAndPresenceSamplerSettings != null)
            {
                yield return new FrequencyAndPresenceSampler(request.FrequencyAndPresenceSamplerSettings);
            }
        }
    }
}