using Llama.Collections.Interfaces;
using Llama.Data;
using Llama.Events;
using LLama.Native;
using System;
using System.Text;

namespace Llama.Context.Interfaces
{
    public interface IContext : IDisposable
    {
        event EventHandler<ContextModificationEventArgs> OnContextModification { add { } remove { } }

        int AvailableBuffer { get; }

        public IReadOnlyLlamaTokenCollection Buffer { get; }

        Encoding Encoding { get; }

        public IReadOnlyLlamaTokenCollection Evaluated { get; }

        SafeLLamaContextHandle Handle { get; }

        int Size { get; }

        void Clear();

        int Evaluate(int count = -1);

        void PostProcess();

        SampleResult SampleNext(IReadOnlyLlamaTokenCollection thisCall);

        void Write(LlamaToken token);
    }
}