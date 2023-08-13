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

        public static T Peek<T>(this List<T> list) => list[^1];

        public static T Pop<T>(this List<T> list)
        {
            T item = list[^1];
            list.RemoveAt(list.Count - 1);
            return item;
        }

        public static void Push<T>(this List<T> list, T item) => list.Add(item);
    }
}