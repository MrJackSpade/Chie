namespace LlamaApi.Shared.Models.Response
{
    public class ContextSnapshotResponse
    {
        public ResponseLlamaToken[] Tokens { get; set; } = Array.Empty<ResponseLlamaToken>();
    }
}