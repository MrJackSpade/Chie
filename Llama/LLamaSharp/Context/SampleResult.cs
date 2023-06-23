using Llama.Collections.Interfaces;

namespace Llama.Context
{
    public class SampleResult
    {
        public bool IsFinal { get; set; }

        public IReadOnlyLlamaTokenCollection Tokens { get; set; }
    }
}