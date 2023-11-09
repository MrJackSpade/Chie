using Llama.Data.Scheduler;

namespace LlamaApi.Shared.Models.Request
{
    public class GetLogitsRequest
    {
        public Guid ContextId { get; set; }

        public ExecutionPriority Priority { get; set; }
    }
}