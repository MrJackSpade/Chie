namespace LlamaApi.Utils
{
    public class ThreadManager
    {
        private readonly object _lock = new();

        private readonly Dictionary<Guid, Thread> _threads = new();

        public void Execute(Action action)
        {
            Guid guid = Guid.NewGuid();

            Thread t = new(() =>
            {
                action.Invoke();
                this._threads.Remove(guid);
            });

            lock (this._lock)
            {
                this._threads.Add(guid, t);
            }

            t.Start();
        }
    }
}