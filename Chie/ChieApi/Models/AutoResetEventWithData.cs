namespace ChieApi.Models
{
    public class AutoResetEventWithData<T>
    {
        private readonly AutoResetEvent _event;

        private T _data;

        public AutoResetEventWithData(bool v = false)
        {
            _event = new(v);
        }

        public void SetDataAndSet(T data)
        {
            lock (this)
            {
                this._data = data;
                this._event.Set();
            }
        }

        public T WaitOneAndGet()
        {
            this._event.WaitOne();
            lock (this)
            {
                return this._data;
            }
        }
    }
}