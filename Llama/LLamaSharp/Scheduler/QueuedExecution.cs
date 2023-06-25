using Llama.Scheduler.Interfaces;
using System;
using System.Threading;

namespace Llama.Scheduler
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

        public T Result { get; private set; }

        public void Execute()
        {
            this.Result = this._func();
            this._completed.Set();
        }

        public void Wait() => this._completed.WaitOne();
    }

    public class QueuedExecution : IQueuedExecution
    {
        private readonly ManualResetEvent _completed;

        private readonly Action _action;

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