using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaKvCell
    {
        /// <summary>
        /// Position in the attention mechanism.
        /// </summary>
        public int pos;

        /// <summary>
        /// Delta value for the position.
        /// </summary>
        public int delta;

        /// <summary>
        /// Used by recurrent state models to copy states.
        /// </summary>
        public int src;

        /// <summary>
        /// Token value.
        /// </summary>
        public int value;

        /// <summary>
        /// Address of the sequence id
        /// </summary>
        public long seq_id;

        /// <summary>
        /// First padding
        /// </summary>
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