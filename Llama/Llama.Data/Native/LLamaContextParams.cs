using Llama.Data.Enums;
using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool GgmlBackendSchedEvalCallback(IntPtr tensor, bool ask, IntPtr userData);

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
        /// RoPE scaling type, from `LlamaRopeScalingType`.
        /// </summary>
        public int RopeScalingType;

        /// <summary>
        /// RoPE base frequency, 0 = from model.
        /// </summary>
        public float RopeFreqBase;

        /// <summary>
        /// RoPE frequency scaling factor, 0 = from model.
        /// </summary>
        public float RopeFreqScale;

        /// <summary>
        /// YaRN extrapolation mix factor, NaN = from model.
        /// </summary>
        public float YarnExtFactor;

        /// <summary>
        /// YaRN magnitude scaling factor.
        /// </summary>
        public float YarnAttnFactor;

        /// <summary>
        /// YaRN low correction dim.
        /// </summary>
        public float YarnBetaFast;

        /// <summary>
        /// YaRN high correction dim.
        /// </summary>
        public float YarnBetaSlow;

        /// <summary>
        /// YaRN original context size.
        /// </summary>
        public uint YarnOrigCtx;

        /// <summary>
        ///
        /// </summary>
        public GgmlBackendSchedEvalCallback CbEval;

        /// <summary>
        ///
        /// </summary>
        public IntPtr CbEvalUserData;

        /// <summary>
        ///
        /// </summary>
        public GgmlType TypeK;

        /// <summary>
        ///
        /// </summary>
        public GgmlType TypeV;

        /// <summary>
        /// If true, use experimental mul_mat_q kernels (DEPRECATED - always true).
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool MulMatQ;

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

        /// <summary>
        /// Embedding mode only.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool OffloadKQV;
    }
}