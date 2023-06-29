using Llama.Native;

namespace Llama.Model.Interfaces
{
    public interface IModelHandleFactory
    {
        SafeLlamaModelHandle Create();
    }
}