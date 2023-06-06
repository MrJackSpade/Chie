using System;
using System.Runtime.InteropServices;

namespace LLama.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct LLamaTokenDataArray
	{
		public Memory<LLamaTokenData> data;
		public ulong size;

		[MarshalAs(UnmanagedType.I1)]
		public bool sorted;

		public LLamaTokenDataArray(LLamaTokenData[] data, ulong size, bool sorted)
		{
			this.data = data;
			this.size = size;
			this.sorted = sorted;
		}
	}
}