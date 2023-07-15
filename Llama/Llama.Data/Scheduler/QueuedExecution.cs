using Llama.Data.Scheduler.Interfaces;

namespace Llama.Data.Scheduler
{
    public class QueuedExecution<T> : IQueuedExecution
    {
        private readonly ManualResetEvent _completed;

        private readonly Func<T> _func;

        public QueuedExecution(Func<T> func)
        {
            this._func = func;
            this._completed = new ManualResetEvent(false);
        }

        public ExecutionResult<T> Result { get; private set; }

        public void Execute()
        {
            try
            {
                this.Result = new ExecutionResult<T> { Value = this._func() };
            }
            catch (Exception ex)
            {
                this.Result = new ExecutionResult<T> { Exception = ex };
            }

            this._completed.Set();
        }

        public void Wait() => this._completed.WaitOne();
    }

    public class QueuedExecution : IQueuedExecution
    {
        private readonly Action _action;

        private readonly ManualResetEvent _completed;

        public QueuedExecution(Action action)
        {
            this._action = action;
            this._completed = new ManualResetEvent(false);
        }

        public void Execute()
        {
            this._action();
            this._completed.Set();
        }

        public void Wait() => this._completed.WaitOne();
    }
}