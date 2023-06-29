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
            _modelHandle = modelHandle;
            _modelSettings = modelSettings;
            _llamaContextSettings = llamaContextSettings;
        }

        public SafeLlamaContextHandle Create() => Utils.InitContextFromParams(_modelHandle, _modelSettings, _llamaContextSettings);
    }
}