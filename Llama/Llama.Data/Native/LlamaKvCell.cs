using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaKvCell
    {
        public int pos;
        public int delta;
        public int value;
        public IntPtr seq_id; // 64-bit representation for the std::set<llama_seq_id>

        public int p1;

        public override string ToString()
        {
            return $"pos: {pos}; delt: {delta}; value {value}";
        }
    }
}