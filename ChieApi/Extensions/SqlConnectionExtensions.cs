using Ai.Utils;
using System.Data.SqlClient;

namespace ChieApi.Extensions
{
	public static class SqlConnectionExtensions
	{
		public static long Insert(this SqlConnection connection, string query, params object[] parameters)
		{
			string parsedQuery = SqlGenerator.GenerateSql(query, parameters);

			using SqlCommand cmd = new(parsedQuery, connection);

			connection.Open();

			int modified = (int)cmd.ExecuteScalar();

			if (connection.State == System.Data.ConnectionState.Open)
			{
				connection.Close();
			}

			return modified;
		}
	}
}