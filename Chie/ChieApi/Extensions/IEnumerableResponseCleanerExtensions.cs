using LlamaApi.Shared.Interfaces;

namespace ChieApi.Extensions
{
    public static class IEnumerableResponseCleanerExtensions
    {
        public static IEnumerable<string> Clean(this IEnumerable<ITextCleaner> responseCleaners, string toClean)
        {
            IEnumerable<string> output = new List<string>()
            {
                toClean
            };

            foreach (ITextCleaner responseCleaner in responseCleaners)
            {
                output = responseCleaner.Clean(output);
            }

            return output.ToList();
        }
    }
}