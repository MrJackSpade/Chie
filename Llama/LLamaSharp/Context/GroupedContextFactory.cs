using Llama.Context.Interfaces;
using System.Collections.Generic;

namespace Llama.Context
{
    public class GroupedContextFactory
    {
        private readonly int _contextBlockCount;

        private readonly IContextFactory _contextFactory;

        private readonly Queue<IContext> _contextQueue;

        public GroupedContextFactory(int contextBlockCount, IContextFactory contextFactory)
        {
            this._contextBlockCount = contextBlockCount;
        }
    }
}