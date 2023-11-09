namespace LlamaApi.Shared.Models.Response
{
    public enum ResponseType : byte
    {
        ContextResponse,
        ContextSnapshotResponse,
        EvaluationResponse,
        GetLogitsResponse,
        ModelResponse,
        PredictResponse,
        TokenizeResponse,
        WriteTokenResponse
    }
}