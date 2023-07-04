using System;

namespace Llama.Native
{
    public class SafeLlamaModelHandle : SafeLlamaHandleBase
    {
        public SafeLlamaModelHandle(IntPtr handle)
            : base(handle)
        {
        }

        protected SafeLlamaModelHandle()
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeApi.FreeModel(this.handle);
            this.SetHandle(IntPtr.Zero);
            return true;
        }
    }
}