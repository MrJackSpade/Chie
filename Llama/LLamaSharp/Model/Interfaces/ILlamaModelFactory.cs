using Llama.Context;
using Llama.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Model.Interfaces
{
    public interface IModelHandleFactory
    {
        SafeLlamaModelHandle Create();
    }
}
