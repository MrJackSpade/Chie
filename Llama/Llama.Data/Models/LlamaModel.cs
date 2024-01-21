using Llama.Data.Native;

namespace Llama.Data.Models
{
    public class LlamaModel : IDisposable
    {
        public LlamaModel(SafeLlamaModelHandle handle, int vocab)
        {
            Vocab = vocab;
            Handle = handle;
        }

        public SafeLlamaModelHandle Handle { get; private set; }

        public int Vocab { get; private set; }

        public void Dispose()
        {
            ((IDisposable)Handle).Dispose();
        }
    }
}