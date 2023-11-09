using Llama.Data.Scheduler;

namespace LlamaApi.Models.Request
{
    public class EvaluateRequest
    {
        public Guid ContextId { get; set; }

        public ExecutionPriority Priority { get; set; }
    }
}