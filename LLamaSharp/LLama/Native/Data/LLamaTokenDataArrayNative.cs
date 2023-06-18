using System.Runtime.InteropServices;

namespace Llama.Native.Data
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LlamaTokenDataArrayNative
    {
        public nint data;

        public ulong size;

        public bool sorted;
    }
}