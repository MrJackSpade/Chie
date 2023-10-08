using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;

namespace ChieApi.Shared.Interfaces
{
	public interface IChieClient
	{
		Task<ContinueRequestResponse> ContinueRequest(string channelId);

		Task<ChatEntry> GetReply(long id);

		Task<ChatEntry[]> GetResponses(string channelId, long after);

		Task<IsTypingResponse> IsTyping(string channel);

		Task<MessageSendResponse> Send(ChatEntry[] chatEntry);

		Task<StartVisibleResponse> StartVisible();
	}
}