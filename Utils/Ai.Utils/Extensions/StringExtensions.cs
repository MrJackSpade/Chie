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

		public static IEnumerable<string> SplitLength(this string source, int length, string breaks = ". ")
		{
			do
			{
				if (source.Length < length)
				{
					yield return source;
					yield break;
				}

				int index = length;
				string chunk = source[..index];

				if (string.IsNullOrWhiteSpace(chunk))
				{
					yield break;
				}

				foreach (char c in breaks)
				{
					bool found = false;

					while (index > 0)
					{
						found = c == chunk[^1];

						if (found)
						{
							break;
						}
						else
						{
							index--;
							chunk = source[..index];
						}
					}

					if (found)
					{
						int split = index;

						string newPart = source[..split];

						yield return newPart;

						source = source[split..];

						break;
					}

					index = length;
					chunk = source[..index];
				}
			} while (true);
		}

		public static IEnumerable<string> Trim(this IEnumerable<string> source)
		{
			foreach (string s in source)
			{
				yield return s.Trim();
			}
		}

		public static bool TryGetSubstring(this string source, int start, int length, out string substring)
		{
			if (start + length >= source.Length)
			{
				substring = null;
				return false;
			}
			else
			{
				substring = source.Substring(start, length);
				return true;
			}
		}
	}
}