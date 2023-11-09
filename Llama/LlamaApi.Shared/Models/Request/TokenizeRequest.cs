using Llama.Data.Scheduler;

namespace LlamaApi.Models.Request
{
    public class TokenizeRequest
    {
        public string? Content { get; set; }

        public Guid ContextId { get; set; }

        public ExecutionPriority Priority { get; set; }
    }
}