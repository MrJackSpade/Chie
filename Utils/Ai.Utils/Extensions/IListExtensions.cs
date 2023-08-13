namespace Ai.Utils.Extensions
{
    public static class IListExtensions
    {
        public static List<T> TryTake<T>(this List<T> source, int count)
        {
            int toTake = Math.Min(count, source.Count);

            List<T> result = new();

            for (int i = 0; i < toTake; i++)
            {
                result.Add(source[^1]);
                source.RemoveAt(source.Count - 1);
            }

            return result;
        }
    }
}