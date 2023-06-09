namespace Ai.Utils.Extensions
{
	public static class IAsyncEnumerableExtensions
	{
		public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> source)
		{
			List<T> list = new();

			await foreach (T item in source)
			{
				list.Add(item);
			}

			return list.ToArray();
		}
	}
}