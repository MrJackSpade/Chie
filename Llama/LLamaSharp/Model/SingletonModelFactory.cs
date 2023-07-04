using Llama.Context;
using Llama.Model.Interfaces;
using Llama.Native;
using Llama.Utilities;

namespace Llama.Model
{
    public class SingletonModelFactory : IModelHandleFactory
    {
        private static readonly object _lock = new();

        private static SafeLlamaModelHandle _handle;

        private readonly LlamaContextSettings _llamaContextSettings;

        private readonly LlamaModelSettings _modelSettings;

        public SingletonModelFactory(LlamaModelSettings modelSettings, LlamaContextSettings llamaContextSettings)
        {
            this._llamaContextSettings = llamaContextSettings;
            this._modelSettings = modelSettings;
        }

        public SafeLlamaModelHandle Create()
        {
            lock (_lock)
            {
                _handle ??= Utils.InitModelFromParams(this._modelSettings, this._llamaContextSettings);

                return _handle;
            }
        }
    }
}