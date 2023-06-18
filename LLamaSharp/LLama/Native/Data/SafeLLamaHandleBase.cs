using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace Llama.Native.Data
{
    public class SafeLlamaHandleBase : SafeHandle, IHasNativeContextHandle
    {
        public SafeLlamaHandleBase(IntPtr handle)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this.SetHandle(handle);
        }

        protected SafeLlamaHandleBase()
                    : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected SafeLlamaHandleBase(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            this.SetHandle(handle);
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public IntPtr Pointer => base.handle;

        public SafeHandle SafeHandle => this;

        void IHasNativeContextHandle.SetHandle(IntPtr pointer) => base.SetHandle(pointer);

        public override string ToString() => $"0x{this.handle:x16}";

        internal void SetHandlePublic(IntPtr pointer) => this.SetHandle(pointer);

        protected override bool ReleaseHandle() => (this as IHasNativeContextHandle).ReleaseHandle();
    }
}