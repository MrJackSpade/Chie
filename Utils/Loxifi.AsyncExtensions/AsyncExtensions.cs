namespace Loxifi.AsyncExtensions
{
    public static class AsyncExtensions
    {
        public static async Task<bool> Any<T>(this IAsyncEnumerable<T> source)
        {
            IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator();

            return await enumerator.MoveNextAsync();
        }

        public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> source)
        {
            List<T> list = new();

            await foreach (T item in source)
            {
                list.Add(item);
            }

            return list.ToArray();
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this List<T> list)
        {
            foreach (T? item in list)
            {
                await Task.Yield(); // This ensures the method yields back to the caller
                yield return item;
            }
        }

        public static async Task<T> Single<T>(this IAsyncEnumerable<T> source)
        {
            List<T> list = new();

            await foreach (T item in source)
            {
                list.Add(item);
            }

            return list.Single();
        }

        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> enumerable)
        {
            List<T> toReturn = new();

            await foreach (T token in enumerable)
            {
                toReturn.Add(token);
            }

            return toReturn;
        }

        public static async Task<bool> Any<T>(this Task<IEnumerable<T>> source) => (await source).Any();

        public static async Task<int> Count<T>(this IAsyncEnumerable<T> source)
        {
            int c = 0;

            await foreach (T item in source)
            {
                c++;
            }

            return c;
        }

        public static async Task<int> Count<T>(this Task<IEnumerable<T>> source)
        {
            int c = 0;

            foreach (T item in await source)
            {
                c++;
            }

            return c;
        }
    }
}