using Llama.Context;
using Llama.Model.Interfaces;
using Llama.Native;
using Llama.Utilities;

namespace Llama.Model
{
    public class SingletonModelFactory : IModelHandleFactory
    {
        private readonly LlamaContextSettings _llamaContextSettings;

        private readonly LlamaModelSettings _modelSettings;

        private SafeLlamaModelHandle _handle;

        public SingletonModelFactory(LlamaModelSettings modelSettings, LlamaContextSettings llamaContextSettings)
        {
            this._llamaContextSettings = llamaContextSettings;
            this._modelSettings = modelSettings;
        }

        public SafeLlamaModelHandle Create()
        {
            this._handle ??= Utils.InitModelFromParams(this._modelSettings, this._llamaContextSettings);

            return this._handle;
        }
    }
}