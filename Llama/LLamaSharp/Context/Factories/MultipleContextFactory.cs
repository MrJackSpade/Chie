using Llama.Context.Interfaces;
using Llama.Model;
using Llama.Native;
using Llama.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Context.Factories
{
    public class MultipleContextFactory : IContextHandleFactory
    {
        private readonly SafeLlamaModelHandle _modelHandle;
        private readonly LlamaModelSettings _modelSettings;
        private readonly LlamaContextSettings _llamaContextSettings;
        public MultipleContextFactory(SafeLlamaModelHandle modelHandle, LlamaModelSettings modelSettings, LlamaContextSettings llamaContextSettings)
        {
            _modelHandle = modelHandle;
            _modelSettings = modelSettings;
            _llamaContextSettings = llamaContextSettings;
        }

        public SafeLlamaContextHandle Create() => Utils.InitContextFromParams(_modelHandle, _modelSettings, _llamaContextSettings);
    }
}
