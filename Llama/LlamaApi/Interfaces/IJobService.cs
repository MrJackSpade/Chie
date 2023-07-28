using Llama.Data.Scheduler;
using LlamaApi.Models;
using System.Runtime.CompilerServices;

namespace LlamaApi.Interfaces
{
    public interface IJobService
    {
        Job Enqueue<TResult>(Func<TResult> func, ExecutionPriority priority, [CallerMemberName] string jobKind = "");

        Job Enqueue(Action action, ExecutionPriority priority, [CallerMemberName] string jobKind = "");

        Job? Get(long id);
    }
}