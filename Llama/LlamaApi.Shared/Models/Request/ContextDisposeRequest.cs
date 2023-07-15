using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LlamaApi.Shared.Models.Request
{
    public class ContextDisposeRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }
    }
}
