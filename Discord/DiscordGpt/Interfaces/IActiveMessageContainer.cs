using Discord.WebSocket;
using DiscordGpt.Models;

namespace DiscordGpt.Interfaces
{
    public interface IActiveMessageContainer : ISingletonContainer<ActiveMessage>
    {
        Task Create(ISocketMessageChannel channel, long messageId);

        Task Finalize(string content);
    }
}