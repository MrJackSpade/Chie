namespace ChieApi.Extensions
{
    public static class ListExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this List<T> list)
        {
            foreach (T? item in list)
            {
                await Task.Yield(); // This ensures the method yields back to the caller
                yield return item;
            }
        }
    }
}
