using System;

namespace Llama.Native
{
    public class SafeLlamaContextHandle : SafeLlamaHandleBase
    {
        private readonly SafeLlamaModelHandle _model;

        public SafeLlamaContextHandle(IntPtr contextPtr, SafeLlamaModelHandle model)
            : base(contextPtr)
        {
            _model = model;
        }

        protected SafeLlamaContextHandle()
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeApi.FreeContext(handle);
            this.SetHandle(IntPtr.Zero);
            return true;
        }
    }
}