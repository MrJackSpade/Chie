using System;
using System.Runtime.InteropServices;

namespace LLama.Native
{
    public abstract class SafeLLamaHandleBase : SafeHandle
    {
        private protected SafeLLamaHandleBase()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        private protected SafeLLamaHandleBase(IntPtr handle)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this.SetHandle(handle);
        }

        private protected SafeLLamaHandleBase(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            this.SetHandle(handle);
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public override string ToString()  => $"0x{this.handle:x16}";
    }
}
