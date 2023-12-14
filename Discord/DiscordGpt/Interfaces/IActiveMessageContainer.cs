using Discord.WebSocket;
using DiscordGpt.Models;

namespace DiscordGpt.Interfaces
{
    public interface IActiveMessageContainer : ISingletonContainer<ActiveMessage>
    {
        Task Create(ISocketMessageChannel channel, long chieMessageId, bool startVisible);

        Task Open(ISocketMessageChannel channel, long chieMessageId, ulong discordMessageId, bool startVisible);

        Task Finalize(string content);
    }
}