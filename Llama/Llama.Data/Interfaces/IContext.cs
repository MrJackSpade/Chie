using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;

namespace Llama.Data.Interfaces
{
    public interface IContext
    {
        public int AvailableBuffer { get; }

        IReadOnlyLlamaTokenCollection Buffer { get; }

        IReadOnlyLlamaTokenCollection Evaluated { get; }

        SafeLlamaContextHandle Handle { get; }

        int Size { get; }

        void Clear();

        void Dispose();

        int Evaluate(ExecutionPriority priority, int count = -1);

        LlamaToken SampleNext(Dictionary<int, float> logitBias, ExecutionPriority priority);
        void SetBufferPointer(int startIndex);
        void Write(LlamaToken token);
    }
}