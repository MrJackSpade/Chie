using Llama.Context.Interfaces;

namespace Llama.Context
{
    public class StaticContextFactory : IContextFactory
    {
        private readonly IContext _context;

        public StaticContextFactory(IContext context)
        {
            this._context = context;
        }

        public IContext CreateContext(IHasNativeContextHandle hasNativeContextHandle) => this._context;

        public IContext CreateContext() => this._context;
    }
}