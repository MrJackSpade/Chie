using ChieApi.Shared.Entities;
using Discord.WebSocket;

namespace DiscordGpt
{
	public class QueuedMessage
	{
		public ChatEntry ChatEntry { get; set; }

		public SocketMessage SocketMessage { get; set; }
	}
}