using Llama.Collections.Interfaces;
using Llama.Native.Data;
using LLama.Native;

namespace Llama.Context.Samplers
{
    public class SampleContext
    {
        public LlamaTokenDataArray Candidates { get; set; }

        public SafeLLamaContextHandle ContextHandle { get; set; }

        public IReadOnlyLlamaTokenCollection ContextTokens { get; set; }

        public IReadOnlyLlamaTokenCollection InferrenceTokens { get; set; }
    }
}