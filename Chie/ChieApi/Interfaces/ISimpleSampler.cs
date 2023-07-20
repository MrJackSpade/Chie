using Llama.Data.Collections;

namespace ChieApi.Interfaces
{
    public interface ISimpleSampler
    {
        Task<Dictionary<int, float>> SampleNext(LlamaTokenCollection thisInferrence);
    }
}
