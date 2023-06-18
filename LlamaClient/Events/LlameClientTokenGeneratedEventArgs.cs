using Llama.Data;

namespace Llama.Events
{
    public class LlameClientTokenGeneratedEventArgs : EventArgs
    {
        public LlamaToken Token { get; set; }
    }
}