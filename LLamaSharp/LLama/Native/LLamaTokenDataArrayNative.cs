using System;
using System.Runtime.InteropServices;

namespace Llama.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct LlamaTokenDataArrayNative
	{
		public IntPtr data;

		public ulong size;

		public bool sorted;
	}
}