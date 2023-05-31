namespace Ai.Utils.Extensions
{
	public static class StringExtensions
	{
		public static IEnumerable<string> CleanSplit(this string source, char splitOn = '\n')
		{
			foreach (string chunk in source.Split(splitOn).Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
			{
				yield return chunk;
			}
		}

		public static bool TryGetSubstring(this string source, int start, int length, out string substring)
		{
			if(start + length >= source.Length)
			{
				substring = null;
				return false;
			} else
			{
				substring = source.Substring(start, length);
				return true;	
			}
		}
	}
}