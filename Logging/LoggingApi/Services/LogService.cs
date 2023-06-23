using ChieApi.Shared.Entities;
using LoggingApi.Interfaces;
using Loxifi.Database.Extensions;
using System.Data.SqlClient;

namespace LoggingApi.Services
{
    public class LogService
    {
        private readonly string _connectionString;

        public LogService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
        }

        public List<LogEntry> GetLogs(DateTime startingDateTime)
        {
            using SqlConnection connection = new(this._connectionString);

            List<LogEntry> entries = connection.Query<LogEntry>($"select * from logentry where DateCreated > '{startingDateTime:yyyy-MM-dd HH:mm:ss}'").ToList();

            return entries;
        }

        public List<LogEntry> GetLogs(long startingId)
        {
            using SqlConnection connection = new(this._connectionString);

            List<LogEntry> entries = connection.Query<LogEntry>($"select * from logentry where id > '{startingId}'").ToList();

            return entries;
        }

        public async Task Insert(IEnumerable<LogEntry> entries)
        {
            using SqlConnection connection = new(this._connectionString);

            foreach (LogEntry entry in entries)
            {
                connection.Insert(entry);
            }
        }
    }
}