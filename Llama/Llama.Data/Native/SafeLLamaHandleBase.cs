using System.Runtime.InteropServices;

namespace Llama.Data.Native
{
    public abstract class SafeLlamaHandleBase : SafeHandle
    {
        private readonly Action<IntPtr> _free;

        protected SafeLlamaHandleBase(Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this._free = free;
        }

        protected SafeLlamaHandleBase(IntPtr handle, Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this.SetHandle(handle);
            this._free = free;
        }

        protected SafeLlamaHandleBase(IntPtr handle, bool ownsHandle, Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle)
        {
            this._free = free;
            this.SetHandle(handle);
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public override string ToString() => $"0x{this.handle:x16}";

        protected override sealed bool ReleaseHandle()
        {
            this._free(this.handle);
            this.SetHandle(IntPtr.Zero);
            return true;
        }
    }
}