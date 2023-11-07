using Llama.Core;
using Llama.Data;
using Llama.Data.Models;
using LlamaApi.Exceptions;

namespace LlamaApi.Models
{
    public class LoadedModel
    {
        private readonly SemaphoreSlim _semaphore = new(1);

        public Dictionary<Guid, ContextInstance> Evaluator { get; } = new Dictionary<Guid, ContextInstance>();

        public Guid Id { get; set; }

        public LlamaModel? Instance { get; set; }

        public LlamaModelSettings? Settings { get; set; }

        public ContextInstance GetContext(Guid id)
        {
            if (this.Instance is null)
            {
                throw new ModelNotLoadedException();
            }

            if (!this.Evaluator.TryGetValue(id, out ContextInstance? context))
            {
                throw new ContextNotFoundException();
            }

            return context;
        }

        public bool TryGetContext(Guid id, out ContextInstance? context)
        {
            if (this.Instance is null)
            {
                throw new ModelNotLoadedException();
            }

            return this.Evaluator.TryGetValue(id, out context);
        }

        public void Lock() => this._semaphore.Wait();

        public void Unlock() => this._semaphore.Release();
    }
}