using Llama.Data.Native;

namespace Llama.Data.Models
{
    public class LlamaModel
    {
        public LlamaModel(SafeLlamaModelHandle handle)
        {
            this.Handle = handle;
        }

        public SafeLlamaModelHandle Handle { get; private set; }
    }
}
