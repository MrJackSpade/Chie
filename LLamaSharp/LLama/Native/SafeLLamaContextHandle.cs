using System;

namespace LLama.Native
{
	public class SafeLLamaContextHandle : SafeLLamaHandleBase
	{
		public SafeLLamaContextHandle(IntPtr handle)
			: base(handle)
		{
		}

		protected SafeLLamaContextHandle()
		{
		}

		protected override bool ReleaseHandle()
		{
			NativeApi.llama_free(this.handle);
			this.SetHandle(IntPtr.Zero);
			return true;
		}
	}
}