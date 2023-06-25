using System;

namespace Llama.Scheduler
{
    public static class IExecutionSchedulerExtensions
    {
        public static T ExecuteBackground<T>(this IExecutionScheduler executionScheduler, Func<T> func) => executionScheduler.Execute(func, ExecutionPriority.Background);

        public static T ExecuteHigh<T>(this IExecutionScheduler executionScheduler, Func<T> func) => executionScheduler.Execute(func, ExecutionPriority.High);

        public static T ExecuteImmediate<T>(this IExecutionScheduler executionScheduler, Func<T> func) => executionScheduler.Execute(func, ExecutionPriority.Immediate);

        public static T ExecuteLow<T>(this IExecutionScheduler executionScheduler, Func<T> func) => executionScheduler.Execute(func, ExecutionPriority.Low);

        public static T ExecuteMedium<T>(this IExecutionScheduler executionScheduler, Func<T> func) => executionScheduler.Execute(func, ExecutionPriority.Medium);
    }
}