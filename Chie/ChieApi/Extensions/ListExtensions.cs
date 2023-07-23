namespace ChieApi.Extensions
{
    public static class ListExtensions
    {
        public static T Dequeue<T>(this List<T> list)
        {
            T item = list[0];
            list.RemoveAt(0);
            return item;
        }

        public static void Enqueue<T>(this List<T> list, T item) => list.Add(item);
    }
}
