using Llama.Data.Collections;
using Llama.Data.Interfaces;

namespace ChieApi.Interfaces
{
    public interface ISimpleSampler
    {
        Task<Dictionary<int, float>> SampleNext(IReadOnlyLlamaTokenCollection thisInferrence);
    }
}
