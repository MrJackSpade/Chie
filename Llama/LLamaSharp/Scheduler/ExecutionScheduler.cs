using Llama.Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Llama.Scheduler
{
    public class ExecutionScheduler : IExecutionScheduler
    {
        private readonly ManualResetEvent _executionGate = new(false);

        private readonly Thread _executionThread;

        private readonly object _lock = new();

        private readonly PriorityQueue<IQueuedExecution, ExecutionPriority> _priorityQueue = new();

        public ExecutionScheduler()
        {
            _executionThread = new Thread(this.ExecutionLoop);
            _executionThread.Start();
        }

        public T Execute<T>(Func<T> action, ExecutionPriority priority)
        {
            QueuedExecution<T> queuedExecution = new(action);

            this.Enqueue(queuedExecution, priority);

            queuedExecution.Wait();

            return queuedExecution.Result;
        }

        private void Enqueue(IQueuedExecution queuedExecution, ExecutionPriority priority)
        {
            lock(_lock)
            {
                _priorityQueue.Enqueue(queuedExecution, priority);
                this._executionGate.Set();
            }
        }
        public void Execute(Action action, ExecutionPriority priority)
        {
            QueuedExecution queuedExecution = new(action);

            this.Enqueue(queuedExecution, priority);

            queuedExecution.Wait();
        }

        private void ExecutionLoop()
        {
            do
            {
                _executionGate.WaitOne();

                IQueuedExecution toExecute;

                lock (_lock)
                {
                    if (!_priorityQueue.TryDequeue(out toExecute, out _))
                    {
                        _executionGate.Reset();
                        continue;
                    }
                }

                toExecute.Execute();
            } while (true);
        }
    }
}