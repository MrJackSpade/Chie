using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Llama.Scheduler
{
    public class PrioritySemaphoreCollection
    {
        private readonly HashSet<ExecutionPriority> _executionPriorities = new();

        private readonly List<PrioritySemaphore> _queue = new();

        public PrioritySemaphoreCollection()
        {
            foreach (ExecutionPriority priority in Enum.GetValues(typeof(ExecutionPriority)))
            {
                _executionPriorities.Add(priority);
                _queue.Add(new PrioritySemaphore(priority));
            }
        }

        public PrioritySemaphore this[ExecutionPriority priority]
        {
            get
            {
                for (int i = 0; i < this._queue.Count; i++)
                {
                    PrioritySemaphore item = this._queue[i];
                    if (item.Priority == priority)
                    {
                        return item;
                    }
                }

                throw new ArgumentException();
            }
        }

        public bool Add(ExecutionPriority executionPriority)
        {
            if (this._executionPriorities.Add(executionPriority))
            {
                this._queue.Add(new PrioritySemaphore(executionPriority));
                return true;
            }

            return false;
        }

        public bool Contains(ExecutionPriority executionPriority) => this._executionPriorities.Contains(executionPriority);

        public void Decrement(ExecutionPriority priority) => this[priority].WaitOne();

        /// <summary>
        /// Allow passage
        /// </summary>
        /// <param name="executionPriority"></param>
        public void Increment(ExecutionPriority priority) => this[priority].Release();

        public ExecutionPriority? WaitAny(ExecutionPriority minPriority, int ms = 0)
        {
            PrioritySemaphore[] toWait = this._queue.Where(e => e.Priority <= minPriority).ToArray();
            int r = WaitHandle.WaitAny(toWait.Select(s => s.WaitHandle).ToArray(), ms);

            if (r == WaitHandle.WaitTimeout)
            {
                return null;
            }

            return toWait[r].Priority;
        }

        public ExecutionPriority? WaitAny(int ms)
        {
            int r = WaitHandle.WaitAny(this._queue.Select(s => s.WaitHandle).ToArray(), ms);

            if (r == WaitHandle.WaitTimeout)
            {
                return null;
            }

            return this._queue[r].Priority;
        }
    }
}