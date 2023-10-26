using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    /// <summary>
    /// Represents the parameters for a llama context.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaContextParams
    {
        /// <summary>
        /// RNG seed, -1 for random.
        /// </summary>
        public uint Seed;

        /// <summary>
        /// Text context, 0 = from model.
        /// </summary>
        public uint NCtx;

        /// <summary>
        /// Prompt processing maximum batch size.
        /// </summary>
        public uint NBatch;

        /// <summary>
        /// Number of threads to use for generation.
        /// </summary>
        public uint NThreads;

        /// <summary>
        /// Number of threads to use for batch processing.
        /// </summary>
        public uint NThreadsBatch;

        /// <summary>
        /// RoPE base frequency, 0 = from model.
        /// </summary>
        public float RopeFreqBase;

        /// <summary>
        /// RoPE frequency scaling factor, 0 = from model.
        /// </summary>
        public float RopeFreqScale;

        /// <summary>
        /// If true, use experimental mul_mat_q kernels.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool MulMatQ;

        /// <summary>
        /// Use fp16 for KV cache, fp32 otherwise.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool F16Kv;

        /// <summary>
        /// The llama_eval() call computes all logits, not just the last one.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool LogitsAll;

        /// <summary>
        /// Embedding mode only.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool Embedding;
    }
}
