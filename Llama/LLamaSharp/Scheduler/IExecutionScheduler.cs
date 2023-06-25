using System;

namespace Llama.Scheduler
{
    public interface IExecutionScheduler
    {
        T Execute<T>(Func<T> action, ExecutionPriority priority);
        void Execute(Action action, ExecutionPriority priority);
    }
}