using Llama.Native;

namespace Llama.Context.Interfaces
{
    public interface IContextHandleFactory
    {
        public SafeLlamaContextHandle Create();
    }
}