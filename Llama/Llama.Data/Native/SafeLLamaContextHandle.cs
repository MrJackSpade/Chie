namespace Llama.Data.Native
{
    public class SafeLlamaContextHandle : SafeLlamaHandleBase
    {
        private readonly SafeLlamaModelHandle _model;

        public SafeLlamaContextHandle(IntPtr contextPtr, SafeLlamaModelHandle model, Action<IntPtr> free)
            : base(contextPtr, free)
        {
            this._model = model;
        }

        protected SafeLlamaContextHandle(Action<IntPtr> free) : base(free)
        {
        }
    }
}