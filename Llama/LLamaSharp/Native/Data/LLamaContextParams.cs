using System;
using System.Runtime.InteropServices;

namespace Llama.Native.Data
{
    public delegate void LlamaProgressCallback(float progress, nint ctx);

    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaContextParams
    {
        public LlamaContextParams()
        {
            n_ctx = 0;
            n_batch = 512;
            n_gpu_layers = 0;
            main_gpu = 0;
            tensor_split = new float[16];
            low_vram = false;
            seed = 0;
            f16_kv = false;
            logits_all = false;
            vocab_only = false;
            use_mlock = false;
            use_mmap = false;
            progress_callback = IntPtr.Zero;
            progress_callback_user_data = IntPtr.Zero;
            embedding = false;
        }

        /// <summary>
        /// text context
        /// </summary>
        public int n_ctx;

        /// <summary>
        /// prompt processing batch size
        /// </summary>
        public int n_batch;                         

        /// <summary>
        /// number of layers to store in VRAM
        /// </summary>
        public int n_gpu_layers;

        /// <summary>
        ///  the GPU that is used for scratch and small tensors
        /// </summary>
        public int main_gpu;

        /// <summary>
        /// how to split layers across multiple GPUs
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] tensor_split;

        /// <summary>
        /// use fp16 for KV cache
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool low_vram;

        /// <summary>
        /// RNG seed, -1 for random
        /// </summary>
        public int seed;

        /// <summary>
        /// use fp16 for KV cache
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool f16_kv;

        /// <summary>
        /// the Llama_eval() call computes all logits, not just the last one
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool logits_all;

        /// <summary>
        /// only load the vocabulary, no weights
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool vocab_only;

        /// <summary>
        /// use mmap if possible
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool use_mmap;

        /// <summary>
        /// force system to keep model in RAM
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool use_mlock;

        /// <summary>
        /// embedding mode only
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool embedding;

        /// <summary>
        /// called with a progress value between 0 and 1, pass NULL to disable
        /// </summary>
        public IntPtr progress_callback;

        /// <summary>
        /// context pointer passed to the progress callback
        /// </summary>
        public IntPtr progress_callback_user_data;
    }
}