using Llama.Collections.Interfaces;
using Llama.Data;
using Llama.Events;
using System;
using System.Text;

namespace Llama.Context.Interfaces
{
    public interface IContext : IDisposable, IHasNativeContextHandle
    {
        event Action<ContextModificationEventArgs> OnContextModification { add { } remove { } }

        int AvailableBuffer { get; }

        public IReadOnlyLlamaTokenCollection Buffer { get; }

        Encoding Encoding { get; }

        public IReadOnlyLlamaTokenCollection Evaluated { get; }

        int Size { get; }

        void Clear();

        int Evaluate(int count = -1);

        void PostProcess();

        SampleResult SampleNext(IReadOnlyLlamaTokenCollection thisCall);

        void Write(LlamaToken token);
    }
}