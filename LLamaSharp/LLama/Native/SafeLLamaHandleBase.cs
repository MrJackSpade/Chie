using System;
using System.Runtime.InteropServices;

namespace Llama.Native
{
	public abstract class SafeLlamaHandleBase : SafeHandle
	{
		protected SafeLlamaHandleBase()
			: base(IntPtr.Zero, ownsHandle: true)
		{
		}

		protected SafeLlamaHandleBase(IntPtr handle)
			: base(IntPtr.Zero, ownsHandle: true)
		{
			this.SetHandle(handle);
		}

		protected SafeLlamaHandleBase(IntPtr handle, bool ownsHandle)
			: base(IntPtr.Zero, ownsHandle)
		{
			this.SetHandle(handle);
		}

		public override bool IsInvalid => this.handle == IntPtr.Zero;

		public override string ToString()
			=> $"0x{this.handle:x16}";
	}
}