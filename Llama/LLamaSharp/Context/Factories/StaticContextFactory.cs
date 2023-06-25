using Llama.Context.Interfaces;
using Llama.Native;

namespace Llama.Context.Factories
{
    public class StaticContextFactory : IContextHandleFactory
    {
        private readonly SafeLlamaContextHandle _context;

        public StaticContextFactory(SafeLlamaContextHandle context)
        {
            _context = context;
        }

        public SafeLlamaContextHandle Create() => _context;
    }
}