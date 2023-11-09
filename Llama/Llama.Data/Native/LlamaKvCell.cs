using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaKvCell
    {
        public int pos;

        public int delta;

        public int value;

        public long seq_id;

        public int p1;

#if WINDOWS

#else

        public long P2;

        public long P3;

        public long P4;

        public long P5;

#endif

        public override string ToString()
        {
            return $"pos: {pos}; delt: {delta}; value {value}";
        }
    }
}