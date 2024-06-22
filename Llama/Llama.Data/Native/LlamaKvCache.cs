using Llama.Data.Enums;
using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct LlamaKvCache
	{
		[MarshalAs(UnmanagedType.I1)]
		public bool HasShift;

		[MarshalAs(UnmanagedType.I1)]
		public bool DoDefrag;

		[MarshalAs(UnmanagedType.I1)]
		public bool DoCopy;

		[MarshalAs(UnmanagedType.I1)]
		public bool Recurrent;

		[MarshalAs(UnmanagedType.I1)]
		public bool VTrans;  // The value tensor is transposed

		public uint Head;
		public uint Size;
		public uint Used;
		public uint N;
		public GgmlType TypeK;
		public GgmlType TypeV;

		// This is where it gets tricky. We'll use a pointer for direct memory access:
		public IntPtr CellsPointer; // Points to the std::vector data in memory.
	}
}