using Llama.Context.Interfaces;
using LLama.Native;

namespace Llama.Context
{
    public class StaticContextFactory : IContextFactory
    {
        private readonly IContext _context;

        public StaticContextFactory(IContext context)
        {
            this._context = context;
        }

        public IContext CreateContext(SafeLLamaContextHandle hasNativeContextHandle) => this._context;

        public IContext CreateContext() => this._context;
    }
}