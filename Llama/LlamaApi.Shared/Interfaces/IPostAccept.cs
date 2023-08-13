using LlamaApiClient;

namespace ChieApi.Interfaces
{
    public interface IPostAccept
    {
        void PostAccept(InferenceEnumerator enumerator);
    }
}