using Llama.Data;
using Llama.Data.Scheduler;
using LlamaApi.Shared.Models.Request;

namespace LlamaApi.Models.Request
{
    public class ContextRequest
    {
        public Guid ContextId { get; set; }

        public ContextRequestSettings ContextRequestSettings { get; set; } = new();

        public Guid ModelId { get; set; }

        public ExecutionPriority Priority { get; set; }

        public LlamaContextSettings Settings { get; set; } = new LlamaContextSettings();
    }
}