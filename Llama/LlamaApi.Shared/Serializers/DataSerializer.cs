using Llama.Core.Samplers.Mirostat;
using Llama.Data;
using Llama.Data.Enums;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Data.Scheduler;
using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using LlamaApi.Shared.Models.Request;
using LlamaApi.Shared.Models.Response;
using System.Text;
using MirostatSamplerSettings = LlamaApi.Models.Request.MirostatSamplerSettings;

namespace LlamaApi.Shared.Serializers
{
    public static class DataSerializer
    {
        public static T Deserialize<T>(byte[] data)
        {
            return Deserialize<T>(new BinaryReader(new MemoryStream(data)));
        }

        public static T Deserialize<T>(BinaryReader reader)
        {
            if (typeof(T) == typeof(Guid))
            {
                return (T)(object)DeserializeGuid(reader);
            }

            if (typeof(T) == typeof(PredictRequest))
            {
                return (T)(object)DeserializePredictRequest(reader);
            }

            if (typeof(T) == typeof(LogitRuleCollection))
            {
                return (T)(object)DeserializeLogitRuleCollection(reader);
            }

            if (typeof(T) == typeof(ResponseCollection))
            {
                return (T)(object)DeserializeResponseCollection(reader);
            }

            if (typeof(T) == typeof(RequestCollection))
            {
                return (T)(object)DeserializeRequestCollection(reader);
            }

            if (typeof(T) == typeof(bool))
            {
                return (T)(object)DeserializeBool(reader);
            }

            if (typeof(T) == typeof(ExecutionPriority))
            {
                return (T)(object)DeserializeExecutionPriority(reader);
            }

            if (typeof(T) == typeof(RequestType))
            {
                return (T)(object)DeserializeRequestType(reader);
            }

            if (typeof(T) == typeof(ResponseType))
            {
                return (T)(object)DeserializeResponseType(reader);
            }

            if (typeof(T) == typeof(LogitRule))
            {
                return (T)(object)DeserializeLogitRule(reader);
            }

            if (typeof(T) == typeof(LogitClamp))
            {
                return (T)(object)DeserializeLogitClamp(reader);
            }

            if (typeof(T) == typeof(MemoryMode))
            {
                return (T)(object)DeserializeMemoryMode(reader);
            }

            if (typeof(T) == typeof(ContextDisposeRequest))
            {
                return (T)(object)DeserializeContextDisposeRequest(reader);
            }

            if (typeof(T) == typeof(TemperatureSamplerSettings))
            {
                return (T)(object)DeserializeTemperatureSamplerSettings(reader);
            }

            if (typeof(T) == typeof(RepetitionSamplerSettings))
            {
                return (T)(object)DeserializeRepetitionSamplerSettings(reader);
            }

            if (typeof(T) == typeof(MirostatTempSamplerSettings))
            {
                return (T)(object)DeserializeMirostatTempSamplerSettings(reader);
            }

            if (typeof(T) == typeof(ComplexPresencePenaltySettings))
            {
                return (T)(object)DeserializeComplexPresencePenaltySettings(reader);
            }

            if (typeof(T) == typeof(ContextSnapshotRequest))
            {
                return (T)(object)DeserializeContextSnapshotRequest(reader);
            }

            if (typeof(T) == typeof(EvaluateRequest))
            {
                return (T)(object)DeserializeEvaluateRequest(reader);
            }

            if (typeof(T) == typeof(GetLogitsRequest))
            {
                return (T)(object)DeserializeGetLogitsRequest(reader);
            }

            if (typeof(T) == typeof(MirostatSamplerSettings))
            {
                return (T)(object)DeserializeMirostatSamplerSettings(reader);
            }

            if (typeof(T) == typeof(ModelRequest))
            {
                return (T)(object)DeserializeModelRequest(reader);
            }

            if (typeof(T) == typeof(LlamaModelSettings))
            {
                return (T)(object)DeserializeLlamaModelSettings(reader);
            }

            if (typeof(T) == typeof(RequestLlamaToken))
            {
                return (T)(object)DeserializeRequestLlamaToken(reader);
            }

            if (typeof(T) == typeof(TokenizeRequest))
            {
                return (T)(object)DeserializeTokenizeRequest(reader);
            }

            if (typeof(T) == typeof(WriteTokenType))
            {
                return (T)(object)DeserializeWriteTokenType(reader);
            }

            if (typeof(T) == typeof(List<RequestLlamaToken>))
            {
                return (T)(object)DeserializeListRequestLlamaToken(reader);
            }

            if (typeof(T) == typeof(WriteTokenRequest))
            {
                return (T)(object)DeserializeWriteTokenRequest(reader);
            }

            if (typeof(T) == typeof(ContextResponse))
            {
                return (T)(object)DeserializeContextResponse(reader);
            }

            if (typeof(T) == typeof(ResponseLlamaToken[]))
            {
                return (T)(object)DeserializeResponseLlamaTokenArray(reader);
            }

            if (typeof(T) == typeof(ContextSnapshotResponse))
            {
                return (T)(object)DeserializeContextSnapshotResponse(reader);
            }

            if (typeof(T) == typeof(ContextState))
            {
                return (T)(object)DeserializeContextState(reader);
            }

            if (typeof(T) == typeof(EvaluationResponse))
            {
                return (T)(object)DeserializeEvaluationResponse(reader);
            }

            if (typeof(T) == typeof(GetLogitsResponse))
            {
                return (T)(object)DeserializeGetLogitsResponse(reader);
            }

            if (typeof(T) == typeof(byte[]))
            {
                return (T)(object)DeserializeByteArray(reader);
            }

            if (typeof(T) == typeof(MirostatType))
            {
                return (T)(object)DeserializeMirostatType(reader);
            }

            if (typeof(T) == typeof(ModelResponse))
            {
                return (T)(object)DeserializeModelResponse(reader);
            }

            if (typeof(T) == typeof(PredictResponse))
            {
                return (T)(object)DeserializePredictResponse(reader);
            }

            if (typeof(T) == typeof(ResponseLlamaToken))
            {
                return (T)(object)DeserializeResponseLlamaToken(reader);
            }

            if (typeof(T) == typeof(TokenizeResponse))
            {
                return (T)(object)DeserializeTokenizeResponse(reader);
            }

            if (typeof(T) == typeof(WriteTokenResponse))
            {
                return (T)(object)DeserializeWriteTokenResponse(reader);
            }

            if (typeof(T) == typeof(ContextRequestSettings))
            {
                return (T)(object)DeserializeContextRequestSettings(reader);
            }

            if (typeof(T) == typeof(ContextRequest))
            {
                return (T)(object)DeserializeContextRequest(reader);
            }

            if (typeof(T) == typeof(uint))
            {
                return (T)(object)DeserializeUint(reader);
            }

            if (typeof(T) == typeof(LlamaContextSettings))
            {
                return (T)(object)DeserializeLlamaContextSettings(reader);
            }

            if (typeof(T) == typeof(LogitBias))
            {
                return (T)(object)DeserializeLogitBias(reader);
            }

            if (typeof(T) == typeof(LogitPenalty))
            {
                return (T)(object)DeserializeLogitPenalty(reader);
            }

            if (typeof(T) == typeof(LogitClampType))
            {
                return (T)(object)DeserializeLogitClampType(reader);
            }

            if (typeof(T) == typeof(LogitBiasType))
            {
                return (T)(object)DeserializeLogitBiasType(reader);
            }

            if (typeof(T) == typeof(LlamaRopeScalingType))
            {
                return (T)(object)DeserializeLlamaRopeScalingType(reader);
            }

            if (typeof(T) == typeof(LogitRuleType))
            {
                return (T)(object)DeserializeLogitRuleType(reader);
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)DeserializeInt(reader);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)DeserializeString(reader);
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)DeserializeFloat(reader);
            }

            if (typeof(T) == typeof(LogitRuleLifetime))
            {
                return (T)(object)DeserializeLogitRuleLifetime(reader);
            }

            throw new NotImplementedException();
        }

        public static bool DeserializeBool(BinaryReader reader)
        {
            return reader.Read() != 0;
        }

        public static byte[] DeserializeByteArray(BinaryReader reader)
        {
            return reader.ReadBytes(reader.Read());
        }

        public static ComplexPresencePenaltySettings DeserializeComplexPresencePenaltySettings(BinaryReader reader)
        {
            ComplexPresencePenaltySettings toReturn = new()
            {
                GroupScale = DeserializeFloat(reader),
                LengthScale = DeserializeFloat(reader),
                MinGroupLength = DeserializeInt(reader),
                RepeatTokenPenaltyWindow = DeserializeInt(reader)
            };

            return toReturn;
        }

        public static ContextDisposeRequest DeserializeContextDisposeRequest(BinaryReader reader)
        {
            ContextDisposeRequest request = new()
            {
                ContextId = DeserializeGuid(reader)
            };

            return request;
        }

        public static ContextRequest DeserializeContextRequest(BinaryReader reader)
        {
            return new ContextRequest()
            {
                ContextId = DeserializeGuid(reader),
                ContextRequestSettings = DeserializeContextRequestSettings(reader),
                ModelId = DeserializeGuid(reader),
                Priority = DeserializeExecutionPriority(reader),
                Settings = DeserializeLlamaContextSettings(reader)
            };
        }

        public static ContextRequestSettings DeserializeContextRequestSettings(BinaryReader reader)
        {
            MirostatType mirostatType = DeserializeMirostatType(reader);

            ContextRequestSettings settings = new()
            {
                ComplexPresencePenaltySettings = DeserializeComplexPresencePenaltySettings(reader),
                RepetitionSamplerSettings = DeserializeRepetitionSamplerSettings(reader),
            };

            if(mirostatType == MirostatType.None)
            {
                settings.TemperatureSamplerSettings = DeserializeTemperatureSamplerSettings(reader);
            }

            if(mirostatType is MirostatType.One or MirostatType.Two)
            {
                settings.MirostatSamplerSettings = DeserializeMirostatSamplerSettings(reader);
            }

            if (mirostatType == MirostatType.Three)
            {
                settings.MirostatTempSamplerSettings = DeserializeMirostatTempSamplerSettings(reader);
            }

            return settings;
        }

        public static ContextResponse DeserializeContextResponse(BinaryReader reader)
        {

            return new ContextResponse()
            {
                State = DeserializeContextState(reader)
            };
        }

        public static ContextSnapshotRequest DeserializeContextSnapshotRequest(BinaryReader reader)
        {
            return new ContextSnapshotRequest()
            {
                ContextId = DeserializeGuid(reader),
                Priority = DeserializeExecutionPriority(reader)
            };
        }

        public static ContextSnapshotResponse DeserializeContextSnapshotResponse(BinaryReader reader)
        {
            return new ContextSnapshotResponse()
            {
                Tokens = DeserializeResponseLlamaTokenArray(reader)
            };
        }

        public static ContextState DeserializeContextState(BinaryReader reader)
        {
            return new ContextState()
            {
                AvailableBuffer = DeserializeUint(reader),
                Id = DeserializeGuid(reader),
                IsLoaded = DeserializeBool(reader),
                Size = DeserializeUint(reader)
            };
        }

        public static EvaluateRequest DeserializeEvaluateRequest(BinaryReader reader)
        {
            return new EvaluateRequest()
            {
                ContextId = DeserializeGuid(reader),
                Priority = DeserializeExecutionPriority(reader)
            };
        }

        public static EvaluationResponse DeserializeEvaluationResponse(BinaryReader reader)
        {
            return new EvaluationResponse()
            {
                AvailableBuffer = DeserializeUint(reader),
                Evaluated = DeserializeUint(reader),
                Id = DeserializeGuid(reader),
                IsLoaded = DeserializeBool(reader)
            };
        }

        public static ExecutionPriority DeserializeExecutionPriority(BinaryReader reader)
        {
            return (ExecutionPriority)reader.ReadByte();
        }

        public static float DeserializeFloat(BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        public static GetLogitsRequest DeserializeGetLogitsRequest(BinaryReader reader)
        {
            return new GetLogitsRequest()
            {
                ContextId = DeserializeGuid(reader),
                Priority = DeserializeExecutionPriority(reader)
            };
        }

        public static GetLogitsResponse DeserializeGetLogitsResponse(BinaryReader reader)
        {
            return new GetLogitsResponse()
            {
                Data = DeserializeByteArray(reader)
            };
        }

        public static Guid DeserializeGuid(BinaryReader reader)
        {
            return new(reader.ReadBytes(16));
        }

        public static int DeserializeInt(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        public static List<RequestLlamaToken> DeserializeListRequestLlamaToken(BinaryReader reader)
        {
            int len = reader.ReadInt32();
            List<RequestLlamaToken> tokens = new(len);

            for (int i = 0; i < len; i++)
            {
                tokens.Add(DeserializeRequestLlamaToken(reader));
            }

            return tokens;
        }

        public static MemoryMode DeserializeMemoryMode(BinaryReader reader)
        {
            return (MemoryMode)reader.ReadByte();
        }

        public static LlamaContextSettings DeserializeLlamaContextSettings(BinaryReader reader)
        {
            return new LlamaContextSettings()
            {
                BatchSize = DeserializeUint(reader),
                ContextSize = DeserializeUint(reader),
                EvalThreadCount = DeserializeUint(reader),
                GenerateEmbedding = DeserializeBool(reader),
                LogitRules = DeserializeLogitRuleCollection(reader),
                LoraAdapter = DeserializeString(reader),
                LoraBase = DeserializeString(reader),
                MemoryMode = DeserializeMemoryMode(reader),
                Perplexity = DeserializeBool(reader),
                RopeFrequencyBase = DeserializeFloat(reader),
                RopeFrequencyScaling = DeserializeFloat(reader),
                RopeScalingType = DeserializeLlamaRopeScalingType(reader),
                Seed = DeserializeUint(reader),
                ThreadCount = DeserializeUint(reader),
                YarnAttnFactor = DeserializeFloat(reader),
                YarnBetaFast = DeserializeFloat(reader),
                YarnBetaSlow = DeserializeFloat(reader),
                YarnExtFactor = DeserializeFloat(reader),
                YarnOrigCtx = DeserializeUint(reader)
            };
        }

        public static LlamaModelSettings DeserializeLlamaModelSettings(BinaryReader reader)
        {
            return new LlamaModelSettings()
            {
                GpuLayerCount = DeserializeInt(reader),
                Model = DeserializeString(reader),
                UseMemoryLock = DeserializeBool(reader),
                UseMemoryMap = DeserializeBool(reader)
            };
        }

        public static LlamaRopeScalingType DeserializeLlamaRopeScalingType(BinaryReader reader)
        {
            return (LlamaRopeScalingType)reader.ReadSByte();
        }

        public static LogitBias DeserializeLogitBias(BinaryReader reader)
        {
            return new LogitBias()
            {
                LifeTime = DeserializeLogitRuleLifetime(reader),
                LogitBiasType = DeserializeLogitBiasType(reader),
                LogitId = DeserializeInt(reader),
                Value = DeserializeFloat(reader),
            };
        }

        public static LogitBiasType DeserializeLogitBiasType(BinaryReader reader)
        {
            return (LogitBiasType)reader.ReadByte();
        }

        public static LogitClamp DeserializeLogitClamp(BinaryReader reader)
        {
            return new LogitClamp()
            {
                LifeTime = DeserializeLogitRuleLifetime(reader),
                LogitId = DeserializeInt(reader),
                Type = DeserializeLogitClampType(reader)
            };
        }
        public static RequestType DeserializeRequestType(BinaryReader reader)
        {
            return (RequestType)reader.ReadByte();
        }

        public static ResponseType DeserializeResponseType(BinaryReader reader)
        {
            return (ResponseType)reader.ReadByte();
        }

        public static LogitClampType DeserializeLogitClampType(BinaryReader reader)
        {
            return (LogitClampType)reader.ReadByte();
        }

        public static LogitPenalty DeserializeLogitPenalty(BinaryReader reader)
        {
            return new LogitPenalty()
            {
                LifeTime = DeserializeLogitRuleLifetime(reader),
                LogitId = DeserializeInt(reader),
                Value = DeserializeFloat(reader)
            };
        }

        public static LogitRule DeserializeLogitRule(BinaryReader reader)
        {
            LogitRuleType type = DeserializeLogitRuleType(reader);

            return type switch
            {
                LogitRuleType.Penalty => DeserializeLogitPenalty(reader),
                LogitRuleType.Bias => DeserializeLogitBias(reader),
                LogitRuleType.Clamp => DeserializeLogitClamp(reader),
                _ => throw new NotImplementedException(),
            };
        }

        public static LogitRuleCollection DeserializeLogitRuleCollection(BinaryReader reader)
        {
            int len = reader.ReadInt32();

            LogitRuleCollection logitRules = new();

            for (int i = 0; i < len; i++)
            {
                logitRules.Add(DeserializeLogitRule(reader));
            }

            return logitRules;
        }

        public static LogitRuleLifetime DeserializeLogitRuleLifetime(BinaryReader reader)
        {
            return (LogitRuleLifetime)reader.ReadByte();
        }

        public static LogitRuleType DeserializeLogitRuleType(BinaryReader reader)
        {
            return (LogitRuleType)reader.ReadByte();
        }

        public static MirostatSamplerSettings DeserializeMirostatSamplerSettings(BinaryReader reader)
        {
            return new MirostatSamplerSettings()
            {
                Eta = DeserializeFloat(reader),
                MirostatType = DeserializeMirostatType(reader),
                PreserveWords = DeserializeBool(reader),
                Tau = DeserializeFloat(reader),
                Temperature = DeserializeFloat(reader)
            };
        }

        public static MirostatTempSamplerSettings DeserializeMirostatTempSamplerSettings(BinaryReader reader)
        {
            return new MirostatTempSamplerSettings()
            {
                FactorPreservedWords = DeserializeBool(reader),
                InitialTemperature = DeserializeFloat(reader),
                LearningRate = DeserializeFloat(reader),
                Tfs = DeserializeFloat(reader),
                PreserveWords = DeserializeBool(reader),
                Target = DeserializeFloat(reader),
                TemperatureLearningRate = DeserializeFloat(reader)
            };
        }

        public static MirostatType DeserializeMirostatType(BinaryReader reader)
        {
            return (MirostatType)reader.ReadByte();
        }

        public static ModelRequest DeserializeModelRequest(BinaryReader reader)
        {
            return new ModelRequest()
            {
                ModelId = DeserializeGuid(reader),
                Settings = DeserializeLlamaModelSettings(reader)
            };
        }

        public static ModelResponse DeserializeModelResponse(BinaryReader reader)
        {
            return new ModelResponse()
            {
                Id = DeserializeGuid(reader)
            };
        }

        public static PredictRequest DeserializePredictRequest(BinaryReader reader)
        {
            return new PredictRequest()
            {
                ContextId = DeserializeGuid(reader),
                LogitRules = DeserializeLogitRuleCollection(reader),
                NewPrediction = DeserializeBool(reader),
                Priority = DeserializeExecutionPriority(reader)
            };
        }

        public static PredictResponse DeserializePredictResponse(BinaryReader reader)
        {
            return new PredictResponse()
            {
                Predicted = DeserializeResponseLlamaToken(reader),
            };
        }

        public static RepetitionSamplerSettings DeserializeRepetitionSamplerSettings(BinaryReader reader)
        {
            return new RepetitionSamplerSettings()
            {
                FrequencyPenalty = DeserializeFloat(reader),
                PresencePenalty = DeserializeFloat(reader),
                RepeatPenalty = DeserializeFloat(reader),
                RepeatTokenPenaltyWindow = DeserializeInt(reader)
            };
        }

        public static RequestCollection DeserializeRequestCollection(BinaryReader reader)
        {
            int len = reader.ReadInt32();

            RequestCollection request = new();

            for (int i = 0; i < len; i++)
            {
                object toAdd;

                RequestType requestType = DeserializeRequestType(reader);

                switch (requestType)
                {
                    case RequestType.ContextDisposeRequest:
                        toAdd = DeserializeContextDisposeRequest(reader);
                        break;
                    case RequestType.ContextRequest:
                        toAdd = DeserializeContextRequest(reader);
                        break;
                    case RequestType.ContextSnapshotRequest:
                        toAdd = DeserializeContextSnapshotRequest(reader);
                        break;
                    case RequestType.EvaluateRequest:
                        toAdd = DeserializeEvaluateRequest(reader);
                        break;
                    case RequestType.GetLogitsRequest:
                        toAdd = DeserializeGetLogitsRequest(reader);
                        break;
                    case RequestType.ModelRequest:
                        toAdd = DeserializeModelRequest(reader);
                        break;
                    case RequestType.PredictRequest:
                        toAdd = DeserializePredictRequest(reader);
                        break;
                    case RequestType.TokenizeRequest:
                        toAdd = DeserializeTokenizeRequest(reader);
                        break;
                    case RequestType.WriteTokenRequest:
                        toAdd = DeserializeWriteTokenRequest(reader);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                request.Requests.Add(toAdd);
            }

            return request;
        }

        public static RequestLlamaToken DeserializeRequestLlamaToken(BinaryReader reader)
        {
            return new RequestLlamaToken()
            {
                TokenId = DeserializeInt(reader)
            };
        }

        public static ResponseCollection DeserializeResponseCollection(BinaryReader reader)
        {
            int len = reader.ReadInt32();

            ResponseCollection response = new();

            for (int i = 0; i < len; i++)
            {
                object toAdd;

                ResponseType responseType = DeserializeResponseType(reader);

                switch (responseType)
                {
                    case ResponseType.ContextResponse:
                        toAdd = DeserializeContextResponse(reader);
                        break;
                    case ResponseType.ContextSnapshotResponse:
                        toAdd = DeserializeContextSnapshotResponse(reader);
                        break;
                    case ResponseType.EvaluationResponse:
                        toAdd = DeserializeEvaluationResponse(reader);
                        break;
                    case ResponseType.GetLogitsResponse:
                        toAdd = DeserializeGetLogitsResponse(reader);
                        break;
                    case ResponseType.ModelResponse:
                        toAdd = DeserializeModelResponse(reader);
                        break;
                    case ResponseType.PredictResponse:
                        toAdd = DeserializePredictResponse(reader);
                        break;
                    case ResponseType.TokenizeResponse:
                        toAdd = DeserializeTokenizeResponse(reader);
                        break;
                    case ResponseType.WriteTokenResponse:
                        toAdd = DeserializeWriteTokenResponse(reader);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                response.Responses.Add(toAdd);
            }

            return response;
        }

        public static ResponseLlamaToken DeserializeResponseLlamaToken(BinaryReader reader)
        {
            return new ResponseLlamaToken()
            {
                Id = reader.ReadInt32(),
                Value = DeserializeString(reader)
            };
        }

        public static ResponseLlamaToken[] DeserializeResponseLlamaTokenArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();

            ResponseLlamaToken[] responseLlamaTokens = new ResponseLlamaToken[length];

            for (int i = 0; i < length; i++)
            {
                responseLlamaTokens[i] = DeserializeResponseLlamaToken(reader);
            }

            return responseLlamaTokens;
        }

        public static string DeserializeString(BinaryReader reader)
        {
            StringBuilder sb = new();

            char c;
            while ((c = reader.ReadChar()) != '\0')
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static TemperatureSamplerSettings DeserializeTemperatureSamplerSettings(BinaryReader reader)
        {
            return new TemperatureSamplerSettings()
            {
                Temperature = DeserializeFloat(reader),
                TfsZ = DeserializeFloat(reader),
                TopK = DeserializeInt(reader),
                TopP = DeserializeFloat(reader),
                TypicalP = DeserializeFloat(reader)
            };
        }

        public static TokenizeRequest DeserializeTokenizeRequest(BinaryReader reader)
        {
            return new TokenizeRequest()
            {
                Content = DeserializeString(reader),
                ContextId = DeserializeGuid(reader),
                Priority = DeserializeExecutionPriority(reader)
            };
        }

        public static TokenizeResponse DeserializeTokenizeResponse(BinaryReader reader)
        {
            return new TokenizeResponse()
            {
                Tokens = DeserializeResponseLlamaTokenArray(reader)
            };
        }

        public static uint DeserializeUint(BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        public static WriteTokenRequest DeserializeWriteTokenRequest(BinaryReader reader)
        {
            return new WriteTokenRequest()
            {
                ContextId = DeserializeGuid(reader),
                Priority = DeserializeExecutionPriority(reader),
                StartIndex = DeserializeInt(reader),
                Tokens = DeserializeListRequestLlamaToken(reader),
                WriteTokenType = DeserializeWriteTokenType(reader)

            };
        }

        public static WriteTokenResponse DeserializeWriteTokenResponse(BinaryReader reader)
        {
            return new WriteTokenResponse()
            {
                State = DeserializeContextState(reader)
            };
        }

        public static byte[] Serialize(ResponseCollection response)
        {
            MemoryStream stream = new();   

            BinaryWriter writer = new(stream);

            Serialize(response, writer);

            return stream.ToArray();
        }

        public static byte[] Serialize(RequestCollection request)
        {
            MemoryStream stream = new();

            BinaryWriter writer = new(stream);

            Serialize(request, writer);

            return stream.ToArray();
        }

        public static void Serialize(ResponseCollection response, BinaryWriter writer)
        {
            List<object> validResponses = response.Responses.Where(r => r != null).ToList();

            int len = validResponses.Count;

            Serialize(len, writer);

            foreach (object o in validResponses)
            {
                if (o is ContextResponse cr)
                {
                    Serialize(ResponseType.ContextResponse, writer);
                    Serialize(cr, writer);
                    continue;
                }

                if (o is ContextSnapshotResponse csr)
                {
                    Serialize(ResponseType.ContextSnapshotResponse, writer);
                    Serialize(csr, writer);
                    continue;
                }

                if (o is EvaluationResponse er)
                {
                    Serialize(ResponseType.EvaluationResponse, writer);
                    Serialize(er, writer);
                    continue;
                }

                if (o is GetLogitsResponse glr)
                {
                    Serialize(ResponseType.GetLogitsResponse, writer);
                    Serialize(glr, writer);
                    continue;
                }

                if (o is ModelResponse mr)
                {
                    Serialize(ResponseType.ModelResponse, writer);
                    Serialize(mr, writer);
                    continue;
                }

                if (o is PredictResponse pr)
                {
                    Serialize(ResponseType.PredictResponse, writer);
                    Serialize(pr, writer);
                    continue;
                }

                if (o is TokenizeResponse tr)
                {
                    Serialize(ResponseType.TokenizeResponse, writer);
                    Serialize(tr, writer);
                    continue;
                }

                if (o is WriteTokenResponse wtr)
                {
                    Serialize(ResponseType.WriteTokenResponse, writer);
                    Serialize(wtr, writer);
                    continue;
                }

                throw new NotImplementedException();
            }
        }

        public static WriteTokenType DeserializeWriteTokenType(BinaryReader reader)
        {
            return (WriteTokenType)reader.ReadByte();
        }

        public static void Serialize(RequestType requestType, BinaryWriter writer)
        {
            writer.Write((byte)requestType);
        }

        public static void Serialize(ResponseType responseType, BinaryWriter writer)
        {
            writer.Write((byte)responseType);
        }

        public static void Serialize(RequestCollection requestCollection, BinaryWriter writer)
        {
            int len = requestCollection.Requests.Count;

            Serialize(len, writer);

            foreach (object o in requestCollection.Requests)
            {
                if (o is ContextDisposeRequest cdr)
                {
                    Serialize(RequestType.ContextDisposeRequest, writer);
                    Serialize(cdr, writer);
                    continue;
                }

                if (o is ContextRequest cr)
                {
                    Serialize(RequestType.ContextRequest, writer);
                    Serialize(cr, writer);
                    continue;
                }

                if (o is ContextSnapshotRequest csr)
                {
                    Serialize(RequestType.ContextSnapshotRequest, writer);
                    Serialize(csr, writer);
                    continue;
                }

                if (o is EvaluateRequest er)
                {
                    Serialize(RequestType.EvaluateRequest, writer);
                    Serialize(er, writer);
                    continue;
                }

                if (o is GetLogitsRequest glr)
                {
                    Serialize(RequestType.GetLogitsRequest, writer);
                    Serialize(glr, writer);
                    continue;
                }

                if (o is ModelRequest mr)
                {
                    Serialize(RequestType.ModelRequest, writer);
                    Serialize(mr, writer);
                    continue;
                }

                if (o is PredictRequest pr)
                {
                    Serialize(RequestType.PredictRequest, writer);
                    Serialize(pr, writer);
                    continue;
                }

                if (o is TokenizeRequest tr)
                {
                    Serialize(RequestType.TokenizeRequest, writer);
                    Serialize(tr, writer);
                    continue;
                }

                if (o is WriteTokenRequest wtr)
                {
                    Serialize(RequestType.WriteTokenRequest, writer);
                    Serialize(wtr, writer);
                    continue;
                }

                throw new NotImplementedException();
            }
        }

        public static void Serialize(Guid request, BinaryWriter writer)
        {
            writer.Write(request.ToByteArray());
        }

        public static void Serialize(PredictRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
            Serialize(request.LogitRules, writer);
            Serialize(request.NewPrediction, writer);
            Serialize(request.Priority, writer);
        }

        public static void Serialize(LogitRuleCollection request, BinaryWriter writer)
        {
            List<LogitRule> rules = request.ToList();

            Serialize(rules.Count, writer);

            foreach (LogitRule rule in rules)
            {
                Serialize(rule.RuleType, writer);
                Serialize(rule, writer);
            }
        }

        public static void Serialize(bool request, BinaryWriter writer)
        {
            writer.Write(request ? (byte)1 : (byte)0);
        }

        public static void Serialize(ExecutionPriority priority, BinaryWriter writer)
        {
            writer.Write((byte)priority);
        }

        public static void Serialize(LogitRule rule, BinaryWriter writer)
        {
            if (rule is LogitBias lb)
            {
                Serialize(lb, writer);
            }

            if (rule is LogitClamp lc)
            {
                Serialize(lc, writer);
            }

            if (rule is LogitPenalty lp)
            {
                Serialize(lp, writer);
            }
        }

        public static void Serialize(LogitClamp request, BinaryWriter writer)
        {
            Serialize(request.LifeTime, writer);
            Serialize(request.LogitId, writer);
            Serialize(request.Type, writer);
        }

        public static void Serialize(ContextDisposeRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
        }

        public static void Serialize(TemperatureSamplerSettings request, BinaryWriter writer)
        {
            Serialize(request.Temperature, writer);
            Serialize(request.TfsZ, writer);
            Serialize(request.TopK, writer);
            Serialize(request.TopP, writer);
            Serialize(request.TypicalP, writer);
        }

        public static void Serialize(RepetitionSamplerSettings request, BinaryWriter writer)
        {
            Serialize(request.FrequencyPenalty, writer);
            Serialize(request.PresencePenalty, writer);
            Serialize(request.RepeatPenalty, writer);
            Serialize(request.RepeatTokenPenaltyWindow, writer);
        }

        public static void Serialize(MirostatTempSamplerSettings request, BinaryWriter writer)
        {
            Serialize(request.FactorPreservedWords, writer);
            Serialize(request.InitialTemperature, writer);
            Serialize(request.LearningRate, writer);
            Serialize(request.Tfs, writer);
            Serialize(request.PreserveWords, writer);
            Serialize(request.Target, writer);
            Serialize(request.TemperatureLearningRate, writer);
        }

        public static void Serialize(MemoryMode request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }

        public static void Serialize(ComplexPresencePenaltySettings request, BinaryWriter writer)
        {
            Serialize(request.GroupScale, writer);
            Serialize(request.LengthScale, writer);
            Serialize(request.MinGroupLength, writer);
            Serialize(request.RepeatTokenPenaltyWindow, writer);
        }

        public static void Serialize(ContextSnapshotRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
            Serialize(request.Priority, writer);
        }

        public static void Serialize(EvaluateRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
            Serialize(request.Priority, writer);
        }

        public static void Serialize(GetLogitsRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
            Serialize(request.Priority, writer);
        }

        public static void Serialize(MirostatSamplerSettings request, BinaryWriter writer)
        {
            Serialize(request.Eta, writer);
            Serialize(request.MirostatType, writer);
            Serialize(request.PreserveWords, writer);
            Serialize(request.Tau, writer);
            Serialize(request.Temperature, writer);
        }

        public static void Serialize(ModelRequest request, BinaryWriter writer)
        {
            Serialize(request.ModelId, writer);
            Serialize(request.Settings, writer);
        }

        public static void Serialize(LlamaModelSettings request, BinaryWriter writer)
        {
            Serialize(request.GpuLayerCount, writer);
            Serialize(request.Model, writer);
            Serialize(request.UseMemoryLock, writer);
            Serialize(request.UseMemoryMap, writer);
        }

        public static void Serialize(RequestLlamaToken request, BinaryWriter writer)
        {
            Serialize(request.TokenId, writer);
        }

        public static void Serialize(TokenizeRequest request, BinaryWriter writer)
        {
            Serialize(request.Content, writer);
            Serialize(request.ContextId, writer);
            Serialize(request.Priority, writer);
        }

        public static void Serialize(WriteTokenType request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }

        public static void Serialize(List<RequestLlamaToken> request, BinaryWriter writer)
        {
            Serialize(request.Count, writer);
            foreach (RequestLlamaToken token in request)
            {
                Serialize(token, writer);
            }
        }

        public static void Serialize(WriteTokenRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
            Serialize(request.Priority, writer);
            Serialize(request.StartIndex, writer);
            Serialize(request.Tokens, writer);
            Serialize(request.WriteTokenType, writer);
        }

        public static void Serialize(ContextResponse request, BinaryWriter writer)
        {
            Serialize(request.State, writer);
        }

        public static void Serialize(ResponseLlamaToken[] request, BinaryWriter writer)
        {
            Serialize(request.Length, writer);
            foreach (ResponseLlamaToken token in request)
            {
                Serialize(token, writer);
            }
        }

        public static void Serialize(ContextSnapshotResponse request, BinaryWriter writer)
        {
            Serialize(request.Tokens, writer);
        }

        public static void Serialize(ContextState request, BinaryWriter writer)
        {
            Serialize(request.AvailableBuffer, writer);
            Serialize(request.Id, writer);
            Serialize(request.IsLoaded, writer);
            Serialize(request.Size, writer);
        }

        public static void Serialize(EvaluationResponse request, BinaryWriter writer)
        {
            Serialize(request.AvailableBuffer, writer);
            Serialize(request.Evaluated, writer);
            Serialize(request.Id, writer);
            Serialize(request.IsLoaded, writer);
        }

        public static void Serialize(GetLogitsResponse request, BinaryWriter writer)
        {
            Serialize(request.Data, writer);
        }

        public static void Serialize(byte[] request, BinaryWriter writer)
        {
            writer.Write(request.Length);
            writer.Write(request);
        }

        public static void Serialize(MirostatType request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }

        public static void Serialize(ModelResponse request, BinaryWriter writer)
        {
            Serialize(request.Id, writer);
        }

        public static void Serialize(PredictResponse request, BinaryWriter writer)
        {
            Serialize(request.Predicted, writer);
        }

        public static void Serialize(ResponseLlamaToken request, BinaryWriter writer)
        {
            Serialize(request.Id, writer);
            Serialize(request.Value, writer);
        }

        public static void Serialize(TokenizeResponse request, BinaryWriter writer)
        {
            Serialize(request.Tokens, writer);
        }

        public static void Serialize(WriteTokenResponse request, BinaryWriter writer)
        {
            Serialize(request.State, writer);
        }

        public static void Serialize(ContextRequestSettings request, BinaryWriter writer)
        {
            MirostatType type = MirostatType.None;

            if(request.MirostatSamplerSettings != null)
            {
                type = request.MirostatSamplerSettings.MirostatType;
            }

            if(request.MirostatTempSamplerSettings != null)
            {
                type = MirostatType.Three;
            }

            Serialize(type, writer);
            Serialize(request.ComplexPresencePenaltySettings, writer);
            Serialize(request.RepetitionSamplerSettings, writer);

            switch (type)
            {
                case MirostatType.None:
                    Serialize(request.TemperatureSamplerSettings, writer);
                    break;
                case MirostatType.One:
                case MirostatType.Two:
                    Serialize(request.MirostatSamplerSettings!, writer);
                    break;
                case MirostatType.Three:
                    Serialize(request.MirostatTempSamplerSettings!, writer);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void Serialize(ContextRequest request, BinaryWriter writer)
        {
            Serialize(request.ContextId, writer);
            Serialize(request.ContextRequestSettings, writer);
            Serialize(request.ModelId, writer);
            Serialize(request.Priority, writer);
            Serialize(request.Settings, writer);
        }

        public static void Serialize(uint u, BinaryWriter writer)
        {
            writer.Write(u);
        }

        public static void Serialize(LlamaContextSettings request, BinaryWriter writer)
        {
            Serialize(request.BatchSize, writer);
            Serialize(request.ContextSize, writer);
            Serialize(request.EvalThreadCount, writer);
            Serialize(request.GenerateEmbedding, writer);
            Serialize(request.LogitRules, writer);
            Serialize(request.LoraAdapter, writer);
            Serialize(request.LoraBase, writer);
            Serialize(request.MemoryMode, writer);
            Serialize(request.Perplexity, writer);
            Serialize(request.RopeFrequencyBase, writer);
            Serialize(request.RopeFrequencyScaling, writer);
            Serialize(request.RopeScalingType, writer);
            Serialize(request.Seed, writer);
            Serialize(request.ThreadCount, writer);
            Serialize(request.YarnAttnFactor, writer);
            Serialize(request.YarnBetaFast, writer);
            Serialize(request.YarnBetaSlow, writer);
            Serialize(request.YarnExtFactor, writer);
            Serialize(request.YarnOrigCtx, writer);
        }

        public static void Serialize(LogitBias request, BinaryWriter writer)
        {
            Serialize(request.LifeTime, writer);
            Serialize(request.LogitBiasType, writer);
            Serialize(request.LogitId, writer);
            Serialize(request.Value, writer);
        }

        public static void Serialize(LogitPenalty request, BinaryWriter writer)
        {
            Serialize(request.LifeTime, writer);
            Serialize(request.LogitId, writer);
            Serialize(request.Value, writer);
        }

        public static void Serialize(LogitClampType request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }

        public static void Serialize(LlamaRopeScalingType request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }

        public static void Serialize(LogitBiasType request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }

        public static void Serialize(LogitRuleType logitRuleType, BinaryWriter writer)
        {
            writer.Write((byte)logitRuleType);
        }

        public static void Serialize(int i, BinaryWriter writer)
        {
            writer.Write(i);
        }

        public static void Serialize(string? s, BinaryWriter writer)
        {
            if (s is not null)
            {
                foreach (char c in s)
                {
                    writer.Write(c);
                }
            }

            writer.Write('\0');
        }

        public static void Serialize(float f, BinaryWriter writer)
        {
            writer.Write(f);
        }

        public static void Serialize(LogitRuleLifetime request, BinaryWriter writer)
        {
            writer.Write((byte)request);
        }
    }
}