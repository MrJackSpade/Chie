using Llama.Context.Interfaces;
using Llama.Model;
using Llama.Native;
using Llama.Utilities;

namespace Llama.Context.Factories
{
    public class MultipleContextFactory : IContextHandleFactory
    {
        private readonly LlamaContextSettings _llamaContextSettings;

        private readonly SafeLlamaModelHandle _modelHandle;

        private readonly LlamaModelSettings _modelSettings;

        public MultipleContextFactory(SafeLlamaModelHandle modelHandle, LlamaModelSettings modelSettings, LlamaContextSettings llamaContextSettings)
        {
            this._modelHandle = modelHandle;
            this._modelSettings = modelSettings;
            this._llamaContextSettings = llamaContextSettings;
        }

        public SafeLlamaContextHandle Create() => Utils.InitContextFromParams(this._modelHandle, this._modelSettings, this._llamaContextSettings);
    }
}