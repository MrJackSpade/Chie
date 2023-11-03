using Llama.Core.Tests.Extensions;
using Llama.Core.Utils;
using Llama.Data.Collections;

namespace Llama.Core.Tests.TestObjects
{
    internal class ShifterTestHarness
    {
        public ShifterTestHarness(char[] evaluated, char[] buffer, int bufferPointer = -1, uint batchSize = 2)
        {
            uint len = (uint)Math.Max(evaluated.Length, buffer.Length);
            uint bp = (uint)(bufferPointer == -1 ? buffer.Length : bufferPointer);
            Evaluated = new KvCacheState<char>(evaluated, '\0');
            Buffer = new PointerArray<char>(len, buffer)
            {
                Pointer = bp
            };

            Shifter = new ArrayShifter<char>(Evaluated);
            Synchronizer = new PointerArraySynchronizer<char>(Shifter, batchSize, '\0');
        }

        public int Pointer => (int)Buffer.Pointer;

        public PointerArray<char> Buffer { get; private set; }

        public KvCacheState<char> Evaluated { get; private set; }

        public ArrayShifter<char> Shifter { get; private set; }

        public PointerArraySynchronizer<char> Synchronizer { get; private set; }

        public static ShifterTestHarness CreateEndExecute(char[] evaluated, char[] buffer, int bufferPointer = -1, uint batchSize = 2)
        {
            ShifterTestHarness shifterTestHarness = new(evaluated, buffer, bufferPointer, batchSize);

            shifterTestHarness.Sync();

            return shifterTestHarness;
        }

        public bool AllMatch()
        {
            return Evaluated.Matches(Buffer) && Evaluated.Matches(Shifter);
        }

        public void Sync()
        {
            Synchronizer.Sync(Evaluated, Buffer);
        }
    }
}