using System;
using System.Runtime.InteropServices;

namespace LLama.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct GgmlInitParams
	{
		public ulong mem_size;
		public IntPtr mem_buffer;

		[MarshalAs(UnmanagedType.I1)]
		public bool no_alloc;
	}
}