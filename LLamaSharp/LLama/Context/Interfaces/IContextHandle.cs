using System;
using System.Runtime.InteropServices;

namespace Llama.Context.Interfaces
{
    public interface IHasNativeContextHandle
    {
        IntPtr Pointer { get; }

        SafeHandle SafeHandle { get; }

        void SetHandle(IntPtr pointer);
    }
}