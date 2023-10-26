using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;

namespace Llama.Data.Interfaces
{
    public interface IContext
    {
        public uint AvailableBuffer { get; }

        IReadOnlyLlamaTokenCollection Buffer { get; }

        IReadOnlyLlamaTokenCollection Evaluated { get; }

        SafeLlamaContextHandle Handle { get; }

        SafeLlamaModelHandle ModelHandle { get; }

        uint Size { get; }

        void Clear();

        void Dispose();

        uint Evaluate(ExecutionPriority priority, int count = -1);

        LlamaToken SampleNext(LogitRuleCollection logitBias, ExecutionPriority priority);

        void SetBufferPointer(uint startIndex);

        void Write(LlamaToken token);
    }
}