﻿using Llama.Data.Interfaces;
using Llama.Data.Native;

namespace Llama.Data.Models
{
    public struct SampleContext
    {
        public LlamaTokenDataArray Candidates { get; set; }

        public SafeLlamaContextHandle ContextHandle { get; set; }

        public IReadOnlyLlamaTokenCollection ContextTokens { get; set; }
    }
}