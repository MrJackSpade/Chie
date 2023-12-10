using LlamaApiClient;

namespace ChieApi.Interfaces
{
    public interface IPostAccept
    {
        Task PostAccept(InferenceEnumerator enumerator);
    }
}