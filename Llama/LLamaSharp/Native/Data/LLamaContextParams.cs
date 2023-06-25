using System;
using System.Runtime.InteropServices;

namespace Llama.Native.Data
{
    public delegate void LlamaProgressCallback(float progress, nint ctx);

    /// <summary>
    /// Represents the parameters for a llama context.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaContextParams
    {
        /// <summary>
        /// RNG seed, -1 for random.
        /// </summary>
        public int seed;

        /// <summary>
        /// Text context.
        /// </summary>
        public int n_ctx;

        /// <summary>
        /// Prompt processing batch size.
        /// </summary>
        public int n_batch;

        /// <summary>
        /// Number of layers to store in VRAM.
        /// </summary>
        public int n_gpu_layers;

        /// <summary>
        /// The GPU that is used for scratch and small tensors.
        /// </summary>
        public int main_gpu;

        /// <summary>
        /// How to split layers across multiple GPUs.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] tensor_split;

        /// <summary>
        /// Called with a progress value between 0 and 1, pass NULL to disable.
        /// </summary>
        public IntPtr progress_callback;

        /// <summary>
        /// Context pointer passed to the progress callback.
        /// </summary>
        public IntPtr progress_callback_user_data;

        /// <summary>
        /// If true, reduce VRAM usage at the cost of performance.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool low_vram;

        /// <summary>
        /// Use fp16 for KV cache.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool f16_kv;

        /// <summary>
        /// The llama_eval() call computes all logits, not just the last one.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool logits_all;

        /// <summary>
        /// Only load the vocabulary, no weights.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool vocab_only;

        /// <summary>
        /// Use mmap if possible.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool use_mmap;

        /// <summary>
        /// Force system to keep model in RAM.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool use_mlock;

        /// <summary>
        /// Embedding mode only.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool embedding;
    }
}