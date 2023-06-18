namespace Llama.Context.Interfaces
{
    public interface IContextFactory
    {
        public IContext CreateContext();

        public IContext CreateContext(IHasNativeContextHandle hasNativeContextHandle);
    }
}