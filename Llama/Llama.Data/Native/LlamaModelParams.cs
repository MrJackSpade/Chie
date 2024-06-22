using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
	using System;
	using System.Runtime.InteropServices;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate bool LlamaProgressCallback(float progress, IntPtr ctx);

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
		/// How to split the model across multiple GPUs.
		/// </summary>
		public LlamaSplitMode SplitMode;

		/// <summary>
		/// The GPU that is used for the entire model or for small tensors and intermediate results.
		/// </summary>
		public int MainGpu;

		/// <summary>
		/// Proportion of the model (layers or rows) to offload to each GPU.
		/// </summary>
		public IntPtr TensorSplit;

		/// <summary>
		/// Comma separated list of RPC servers to use for offloading.
		/// </summary>
		public IntPtr RpcServers;

		/// <summary>
		/// Called with a progress value between 0 and 1, pass NULL to disable.
		/// </summary>
		public LlamaProgressCallback ProgressCallback;

		/// <summary>
		/// Context pointer passed to the progress callback.
		/// </summary>
		public IntPtr ProgressCallbackUserData;

		/// <summary>
		/// Override key-value pairs of the model meta data.
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

		/// <summary>
		/// Validate model tensor data.
		/// </summary>
		[MarshalAs(UnmanagedType.I1)]
		public bool CheckTensors;
	}
}