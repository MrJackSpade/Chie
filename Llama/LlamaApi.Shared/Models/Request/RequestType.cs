namespace LlamaApi.Shared.Models.Request
{
    public enum RequestType : byte
    {
        ContextDisposeRequest,

        ContextRequest,

        ContextSnapshotRequest,

        EvaluateRequest,

        GetLogitsRequest,

        ModelRequest,

        PredictRequest,

        TokenizeRequest,

        WriteTokenRequest
    }
}