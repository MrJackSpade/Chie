using Llama.Data.Scheduler;

namespace LlamaApi.Models.Request
{
    public class ContextSnapshotRequest
    {
        public Guid ContextId { get; set; }

        public ExecutionPriority Priority { get; set; }
    }
}