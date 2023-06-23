using System.Data.SqlClient;
using System.Reflection;

namespace Loxifi.Database.Extensions
{
    public static class SqlClientExtensions
    {
        private static readonly SqlGenerator _sqlGenerator = new();

        public static IEnumerable<T> Query<T>(this SqlConnection connection, string query) where T : new()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            using SqlCommand command = new(query, connection);
            
            using SqlDataReader reader = command.ExecuteReader();

            // Get column names from reader
            List<string> columns = new();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            // Generate instances of T for each row
            while (reader.Read())
            {
                T item = new();
                Type itemType = item.GetType();

                foreach (string column in columns)
                {
                    PropertyInfo? property = itemType.GetProperty(column, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (property != null && property.CanWrite)
                    {
                        // Handle null database values
                        object value = reader[column];
                        property.SetValue(item, value is DBNull ? null : value, null);
                    }
                }

                yield return item;
            }
        }

        public static long Insert<T>(this SqlConnection connection, T toAdd) where T : class
        {
            string parsedQuery = _sqlGenerator.GenerateInsert(toAdd);

            using SqlCommand cmd = new(parsedQuery, connection);

            connection.Open();

            int returnValue = (int)cmd.ExecuteScalar();

            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }

            return returnValue;
        }
    }
}