using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    public delegate void LlamaProgressCallback(float progress, IntPtr ctx);

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
        /// Text context.
        /// </summary>
        public int NCtx;

        /// <summary>
        /// Prompt processing batch size.
        /// </summary>
        public int NBatch;

        /// <summary>
        /// Grouped-query attention (TEMP - will be moved to model hparams).
        /// </summary>
        public int NGqa;

        /// <summary>
        /// rms norm epsilon (TEMP - will be moved to model hparams).
        /// </summary>
        public int RmsNormEps;

        /// <summary>
        /// Number of layers to store in VRAM.
        /// </summary>
        public int NGpuLayers;

        /// <summary>
        /// The GPU that is used for scratch and small tensors.
        /// </summary>
        public int MainGpu;

        /// <summary>
        /// How to split layers across multiple GPUs (size: LLAMA_MAX_DEVICES).
        /// </summary>
        public IntPtr TensorSplit;

        /// <summary>
        /// RoPE base frequency. See: https://github.com/ggerganov/llama.cpp/pull/2054
        /// </summary>
        public float RopeFreqBase;

        /// <summary>
        /// RoPE frequency scaling factor.
        /// </summary>
        public float RopeFreqScale;

        /// <summary>
        /// Called with a progress value between 0 and 1, pass NULL to disable.
        /// </summary>
        public IntPtr ProgressCallback;

        /// <summary>
        /// Context pointer passed to the progress callback.
        /// </summary>
        public IntPtr ProgressCallbackUserData;

        /// <summary>
        /// If true, reduce VRAM usage at the cost of performance.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool LowVram;

        /// <summary>
        /// if true, use experimental mul_mat_q kernels
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool MulMatQ;

        /// <summary>
        /// Use fp16 for KV cache.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool F16Kv;

        /// <summary>
        /// The llama_eval() call computes all logits, not just the last one.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool LogitsAll;

        /// <summary>
        /// Only load the vocabulary, no weights.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool VocabOnly;

        /// <summary>
        /// Use mmap if possible.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseMmap;

        /// <summary>
        /// Force system to keep model in RAM.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseMlock;

        /// <summary>
        /// Embedding mode only.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool Embedding;
    }
}