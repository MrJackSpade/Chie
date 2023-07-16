using Llama.Data.Scheduler;
using LlamaApi.Interfaces;
using LlamaApi.Models;
using LlamaApi.Utils;
using Loxifi.Extensions;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace LlamaApi.Services
{
    public class JobService : IJobService
    {
        private readonly ConcurrentQueue<Job> _cache;

        private readonly string _connectionString;

        private readonly IExecutionScheduler _executionScheduler;

        private readonly ThreadManager _threadPool;

        public JobService(JobServiceSettings settings)
        {
            this._connectionString = settings.HasConnectionString.ConnectionString;
            this._executionScheduler = settings.ExecutionScheduler;
            this._threadPool = settings.ThreadPool;
            this._cache = settings.Cache;
        }

        private SqlConnection NewConnection => new(this._connectionString);

        public Job Enqueue<TResult>(Func<TResult> func, ExecutionPriority priority)
        {
            using SqlConnection newConnect = this.NewConnection;

            Job job = new();

            long id = newConnect.Insert(job)!.Value;

            job.Id = id;

            this._threadPool.Execute(() =>
            {
                try
                {
                    TResult result = this._executionScheduler.Execute(() =>
                    {
                        this.UpdateState(id, JobState.Processing);
                        return func.Invoke();
                    }
                    , priority);

                    this.UpdateResult(id, result);
                }
                catch (Exception ex)
                {
                    this.UpdateResult(id, new JobFailure(ex));
                }

                this.Cache(id);
            });

            return job;
        }

        public Job Enqueue(Action action, ExecutionPriority priority)
        {
            Job job = new();

            using SqlConnection newConnect = this.NewConnection;

            long id = newConnect.Insert(job)!.Value;

            job.Id = id;

            this._threadPool.Execute(() =>
            {
                try
                {
                    this._executionScheduler.Execute(() =>
                    {
                        this.UpdateState(id, JobState.Processing);
                        action.Invoke();
                    }
                    , priority);

                    this.UpdateResult(id, null);
                }
                catch (Exception ex)
                {
                    this.UpdateResult(id, new JobFailure(ex));
                }

                this.Cache(id);
            });

            return job;
        }

        public Job Get(long id)
        {
            try
            {
                foreach (Job job in this._cache.ToList())
                {
                    if (job.Id == id)
                    {
                        return job;
                    }
                }
            }
            catch (Exception)
            {
                //Probably a concurrency issue which we dont actually care about
                //just fall back on the DB
            }

            using SqlConnection newConnect = this.NewConnection;

            return newConnect.Query<Job>($"Select top 1 * from job where id = '{id}'").First();
        }

        private void Cache(long id)
        {
            using SqlConnection newConnect = this.NewConnection;

            Job job = newConnect.Query<Job>($"select top 1 * from job where id = {id}").First();

            this._cache.Enqueue(job);

            while (this._cache.Count > 100)
            {
                this._cache.TryDequeue(out _);
            }
        }

        private void UpdateResult(long id, object? result)
        {
            JobState state = JobState.Success;

            if (result is JobFailure jf)
            {
                state = JobState.Failure;
            }

            StringBuilder queryBuilder = new($"update job set state = {(int)state} ");

            if (result != null)
            {
                string? content = JsonSerializer.Serialize(result);
                queryBuilder.Append($", result = '{content.Replace("'", "''")}'");
            }

            queryBuilder.Append($" where id = {id}");

            string query = queryBuilder.ToString();

            using SqlConnection newConnect = this.NewConnection;

            newConnect.Execute(query);
        }

        private void UpdateState(long id, JobState state)
        {
            using SqlConnection newConnect = this.NewConnection;

            newConnect.Execute($"update job set state = {(int)state} where id = {id}");
        }
    }
}