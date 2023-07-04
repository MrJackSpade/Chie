using Llama.Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Llama.Scheduler
{
    public partial class ExecutionScheduler : IExecutionScheduler
    {
        private readonly Thread _executionThread;

        private readonly object _lock = new();

        private readonly PriorityQueue<IQueuedExecution, ExecutionPriority> _priorityQueue = new();

        private readonly PrioritySemaphoreCollection _priorityResetEvents = new();

        public ExecutionScheduler()
        {
            this._executionThread = new Thread(this.ExecutionLoop);
            this._executionThread.Start();
        }

        public T Execute<T>(Func<T> action, ExecutionPriority priority)
        {
            QueuedExecution<T> queuedExecution = new(action);

            this.Enqueue(queuedExecution, priority);

            queuedExecution.Wait();

            return queuedExecution.Result;
        }

        public void Execute(Action action, ExecutionPriority priority)
        {
            QueuedExecution queuedExecution = new(action);

            this.Enqueue(queuedExecution, priority);

            queuedExecution.Wait();
        }

        private void Enqueue(IQueuedExecution queuedExecution, ExecutionPriority priority)
        {
            lock (this._lock)
            {
                this._priorityQueue.Enqueue(queuedExecution, priority);
                this._priorityResetEvents.Add(priority);
                this._priorityResetEvents.Increment(priority);
            }
        }

        private void ExecutionLoop()
        {
            ExecutionPriority lastPriority = ExecutionPriority.Background;

            do
            {
                //Wait forever if priority lowest
                int ms = lastPriority == ExecutionPriority.Background ? -1 : 50;

                //Try and get a wait handle
                ExecutionPriority? opened = this._priorityResetEvents.WaitAny(lastPriority, ms);

                //No wait handle, nothing of greater or equal priority
                if (opened is null)
                {
                    //Next wait is for anything
                    lastPriority = ExecutionPriority.Background;
                    continue;
                }

                //If we caught something, then we know greater or equal was available
                IQueuedExecution toExecute;

                lock (this._lock)
                {
                    //Try and grab whatever
                    if (!this._priorityQueue.TryDequeue(out toExecute, out lastPriority))
                    {
                        //How did we end up here if we knew something existed?
                        throw new Exception();
                    }

                    //Correct if what we grabbed isn't what we thought we were grabbing
                    if (lastPriority != opened.Value)
                    {
                        this._priorityResetEvents.Decrement(lastPriority);
                        this._priorityResetEvents.Increment(opened.Value);
                    }
                }

                //execute it
                toExecute.Execute();
            } while (true);
        }
    }
}