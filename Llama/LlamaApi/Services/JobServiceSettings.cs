using Llama.Data.Scheduler;
using LlamaApi.Interfaces;
using LlamaApi.Models;
using LlamaApi.Utils;
using System.Collections.Concurrent;

namespace LlamaApi.Services
{
    public class JobServiceSettings
    {
        public JobServiceSettings(IHasConnectionString hasConnectionString)
        {
            this.HasConnectionString = hasConnectionString;
        }

        public ConcurrentQueue<Job> Cache { get; set; } = new ConcurrentQueue<Job>();

        public IExecutionScheduler ExecutionScheduler { get; set; } = new ExecutionScheduler();

        public IHasConnectionString HasConnectionString { get; set; }

        public ThreadManager ThreadPool { get; set; } = new ThreadManager();
    }
}