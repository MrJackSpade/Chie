using ChieApi.Interfaces;

namespace ChieApi.Extensions
{
    public static class IEnumerableResponseCleanerExtensions
    {
        public static string Clean(this IEnumerable<IResponseCleaner> responseCleaners, string toClean)
        {
            string input = toClean;

            foreach (IResponseCleaner responseCleaner in responseCleaners)
            {
                input = responseCleaner.Clean(input);
            }

            return input;
        }
    }
}