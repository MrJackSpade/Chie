using ChieApi.Shared.Entities;

namespace ChieApi.Interfaces
{
    public interface IRequestPipeline
    {
        IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry);
    }
}