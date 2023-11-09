using Llama.Data.Scheduler;

namespace LlamaApi.Models.Request
{
    public class WriteTokenRequest
    {
        public Guid ContextId { get; set; }

        public ExecutionPriority Priority { get; set; }

        public int StartIndex { get; set; } = -1;

        public List<RequestLlamaToken> Tokens { get; set; } = new();

        public WriteTokenType WriteTokenType { get; set; } = WriteTokenType.Overwrite;
    }
}