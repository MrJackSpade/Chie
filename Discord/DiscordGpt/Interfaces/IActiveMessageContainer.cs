using Discord.WebSocket;
using DiscordGpt.Models;

namespace DiscordGpt.Interfaces
{
	public interface IActiveMessageContainer : ISingletonContainer<ActiveMessage>
	{
		Task Create(ISocketMessageChannel channel, long messageId, bool startVisible);

		Task Finalize(string content);
	}
}