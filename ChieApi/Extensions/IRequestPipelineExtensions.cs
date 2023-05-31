using ChieApi.Interfaces;
using ChieApi.Shared.Entities;

namespace ChieApi.Extensions
{
	public static class IRequestPipelineExtensions
	{
		public static async Task<List<ChatEntry>> Process(this IRequestPipeline requestPipeline, List<ChatEntry> chatEntries)
		{
			List<ChatEntry> thisProcessedEntries = new();

			foreach (ChatEntry chatEntry in chatEntries)
			{
				await foreach (ChatEntry thisEntry in requestPipeline.Process(chatEntry))
				{
					thisProcessedEntries.Add(thisEntry);
				}
			}

			return thisProcessedEntries;
		}
	}
}