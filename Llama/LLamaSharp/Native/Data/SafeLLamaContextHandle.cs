using System;

namespace Llama.Native
{
    public class SafeLlamaContextHandle : SafeLlamaHandleBase
    {
        private readonly SafeLlamaModelHandle _model;

        public SafeLlamaContextHandle(IntPtr contextPtr, SafeLlamaModelHandle model)
            : base(contextPtr)
        {
            this._model = model;
        }

        protected SafeLlamaContextHandle()
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeApi.FreeContext(this.handle);
            this.SetHandle(IntPtr.Zero);
            return true;
        }
    }
}