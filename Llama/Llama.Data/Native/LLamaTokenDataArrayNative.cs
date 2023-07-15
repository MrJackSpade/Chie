using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaTokenDataArrayNative
    {
        public nint data;

        public ulong size;

        public bool sorted;
    }
}