using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    public delegate void LlamaProgressCallback(float progress, IntPtr ctx);

    /// <summary>
    /// Represents the model parameters for llama.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaModelParams
    {
        /// <summary>
        /// Number of layers to store in VRAM.
        /// </summary>
        public int NGpuLayers;

        /// <summary>
        /// The GPU that is used for scratch and small tensors.
        /// </summary>
        public int MainGpu;

        /// <summary>
        /// How to split layers across multiple GPUs.
        /// </summary>
        public IntPtr TensorSplit;

        /// <summary>
        /// Called with a progress value between 0 and 1, pass NULL to disable.
        /// </summary>
        public LlamaProgressCallback ProgressCallback;

        /// <summary>
        /// Context pointer passed to the progress callback.
        /// </summary>
        public IntPtr ProgressCallbackUserData;

        /// <summary>
        /// override key-value pairs of the model meta data
        /// </summary>
        public IntPtr KvOverrides;

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
    }
}