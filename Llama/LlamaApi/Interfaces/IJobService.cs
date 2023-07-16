using Llama.Data.Scheduler;
using LlamaApi.Models;

namespace LlamaApi.Interfaces
{
    public interface IJobService
    {
        Job Enqueue<TResult>(Func<TResult> func, ExecutionPriority priority);

        Job Enqueue(Action action, ExecutionPriority priority);

        Job? Get(long id);
    }
}