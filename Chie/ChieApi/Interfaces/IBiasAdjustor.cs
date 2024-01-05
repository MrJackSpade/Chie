using LlamaApiClient;

namespace ChieApi.Interfaces
{
    public interface IBiasAdjustor
    {
        Task AdjustNext(InferenceEnumerator enumerator);
    }
}
