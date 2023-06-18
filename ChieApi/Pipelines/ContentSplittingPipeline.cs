using Ai.Utils.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
    public class ContentSplittingPipeline : IRequestPipeline
    {
        public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
        {
            string content = chatEntry.Content;

            foreach (string newLine in content.CleanSplit())
            {
                yield return chatEntry with { Content = newLine };
            }
        }
    }
}