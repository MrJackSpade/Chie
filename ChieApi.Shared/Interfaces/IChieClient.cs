using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;

namespace ChieApi.Shared.Interfaces
{
	public interface IChieClient
	{

		Task<List<LogEntry>> GetLogsByDate(string after);

		Task<List<LogEntry>> GetLogsById(long after);

		Task<ChatEntry[]> GetResponses(string channelId, long after);

		Task<ChatEntry> GetReply(long id);

		Task<IsTypingResponse> IsTyping(string channel);

		Task<MessageSendResponse> Send(ChatEntry[] chatEntry);

		Task<ContinueRequestResponse> ContinueRequest(string channelId);
	}
}