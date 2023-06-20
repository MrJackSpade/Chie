using Llama.Collections.Interfaces;
using Llama.Native.Data;
using LLama.Native;
using System;

namespace Llama.Context.Samplers
{
    public ref struct SampleContext
    {
        public LlamaTokenDataArray Candidates { get; set; }

        public SafeLLamaContextHandle ContextHandle { get; set; }

        public IReadOnlyLlamaTokenCollection ContextTokens { get; set; }

        public IReadOnlyLlamaTokenCollection InferrenceTokens { get; set; }

        public Span<float> Logits { get; set; }
    }
}