namespace Llama.Data.Native
{
    public class SafeLlamaModelHandle : SafeLlamaHandleBase
    {
        public SafeLlamaModelHandle(IntPtr handle, Action<IntPtr> free)
            : base(handle, free)
        {
        }

        protected SafeLlamaModelHandle(Action<IntPtr> free) : base(free)
        {
        }
    }
}