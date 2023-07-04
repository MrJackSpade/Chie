using Llama.Context.Interfaces;
using Llama.Native;

namespace Llama.Context.Factories
{
    public class StaticContextFactory : IContextHandleFactory
    {
        private readonly SafeLlamaContextHandle _context;

        public StaticContextFactory(SafeLlamaContextHandle context)
        {
            this._context = context;
        }

        public SafeLlamaContextHandle Create() => this._context;
    }
}