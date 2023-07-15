using Llama.Data.Interfaces;

namespace Llama.Data.Models
{
    public class SampleResult
    {
        public bool IsFinal { get; set; }

        public IReadOnlyLlamaTokenCollection Tokens { get; set; }
    }
}