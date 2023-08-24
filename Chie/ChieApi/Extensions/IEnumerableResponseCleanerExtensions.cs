using ChieApi.Interfaces;

namespace ChieApi.Extensions
{
    public static class IEnumerableResponseCleanerExtensions
    {
        public static string Clean(this IEnumerable<ITextCleaner> responseCleaners, string toClean)
        {
            string input = toClean;

            foreach (ITextCleaner responseCleaner in responseCleaners)
            {
                input = responseCleaner.Clean(input);
            }

            return input;
        }
    }
}