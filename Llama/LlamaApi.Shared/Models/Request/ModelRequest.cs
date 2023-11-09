using Llama.Data;

namespace LlamaApi.Models.Request
{
    public class ModelRequest
    {
        public Guid ModelId { get; set; }

        public LlamaModelSettings Settings { get; set; }
    }
}