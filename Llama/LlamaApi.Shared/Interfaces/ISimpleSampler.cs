using LlamaApiClient;

namespace ChieApi.Interfaces
{
    public interface ISimpleSampler
    {
        Task SampleNext(InferenceEnumerator enumerator);
    }
}