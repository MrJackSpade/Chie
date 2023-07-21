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

        public static T Dequeue<T>(this List<T> list)
        {
            T item = list[0];
            list.RemoveAt(0);
            return item;
        }

        public static void Enqueue<T>(this List<T> list, T item) => list.Add(item);
    }
}
