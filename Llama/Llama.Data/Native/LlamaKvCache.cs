using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaKvCache
    {
        public bool has_shift;

        public uint head;

        public uint size;

        public uint used;

        public uint n;

        // This is where it gets tricky. We'll use a pointer for direct memory access:
        public IntPtr cellsPointer; // Points to the std::vector data in memory.

        // Ignoring other fields since you mentioned not needing them.
    }
}