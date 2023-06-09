namespace Ai.Utils
{
	public static class SqlGenerator
	{
		public static string? FormatArgument(object o)
		{
			if (o is null)
			{
				return "null";
			}

			if (o is string s)
			{
				return $"N'{s.Replace("'", "''")}'";
			}

			if (o is DateTime dt)
			{
				return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
			}

			if (o is bool b)
			{
				return b ? "1" : "0";
			}

			return o.ToString();
		}

		public static string GenerateSql(string query, params object[] parameters)
		{
			List<object> convertedValues = new();

			foreach (object parameter in parameters)
			{
				convertedValues.Add(FormatArgument(parameter));
			}

			string result = string.Format(query, convertedValues.ToArray());

			return result;
		}
	}
}