using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaApi.Shared.Interfaces
{
    public interface IContextRequest
    {
        Guid ContextId { get; set; }
    }
}
