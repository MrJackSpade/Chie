namespace Llama.Data.Scheduler
{
    public class PrioritySemaphore
    {
        private readonly Semaphore _semaphore;

        public PrioritySemaphore(ExecutionPriority priority)
        {
            this._semaphore = new Semaphore(0, int.MaxValue);
            this.Priority = priority;
        }

        public PrioritySemaphore() : base()
        {
            this._semaphore = new Semaphore(0, int.MaxValue);
        }

        public int Available { get; private set; }

        public ExecutionPriority Priority { get; private set; }

        public WaitHandle WaitHandle => this._semaphore;

        public void Release()
        {
            this._semaphore.Release();
            this.Available++;
        }

        public override string ToString() => $"{this.Priority}: {this.Available}";

        internal void WaitOne()
        {
            this._semaphore.WaitOne();
            this.Available--;
        }
    }
}