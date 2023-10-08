using Loxifi;

namespace Chie.Extensions
{
	public static class DictionaryFileExtensions
	{
		public static void Add(this DictionaryFile source, ulong key, ulong value) => source.Add(key.ToString(), value.ToString());

		public static bool TryGetValue(this DictionaryFile source, ulong key, out ulong value)
		{
			string skey = $"{key}";

			bool v = source.TryGetValue(skey, out string sv);

			if (v)
			{
				value = ulong.Parse(sv);
			}
			else
			{
				value = 0;
			}

			return v;
		}
	}
}