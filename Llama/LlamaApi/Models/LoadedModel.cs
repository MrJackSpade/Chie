using Llama.Core;
using Llama.Data;
using Llama.Data.Models;
using LlamaApi.Exceptions;

namespace LlamaApi.Models
{
    public class LoadedModel
    {
        private readonly SemaphoreSlim _semaphore = new(1);

        public Dictionary<Guid, ContextEvaluator> Evaluator { get; } = new Dictionary<Guid, ContextEvaluator>();

        public Guid Id { get; set; }

        public LlamaModel? Instance { get; set; }

        public LlamaModelSettings? Settings { get; set; }

        public ContextEvaluator GetContext(Guid id)
        {
            if (this.Instance is null)
            {
                throw new ModelNotLoadedException();
            }

            if (!this.Evaluator.TryGetValue(id, out ContextEvaluator? context))
            {
                throw new ContextNotFoundException();
            }

            return context;
        }

        public void Lock() => this._semaphore.Wait();

        public void Unlock() => this._semaphore.Release();
    }
}