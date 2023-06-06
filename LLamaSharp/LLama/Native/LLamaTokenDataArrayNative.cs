using System;
using System.Runtime.InteropServices;

namespace LLama.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct LLamaTokenDataArrayNative
	{
		public IntPtr data;
		public ulong size;
		public bool sorted;
	}
}