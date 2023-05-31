using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Dapper;
using System.Data.SqlClient;
using LogLevel = ChieApi.Shared.Entities.LogLevel;

namespace ChieApi.Services
{
	public class LogService
	{
		private const string LOG_INSERT = @"INSERT INTO [dbo].[LogEntry]
           ([Level]
           ,[Content]
           ,[DateCreated])
		output INSERTED.ID
		 VALUES
			   ({0}
			   ,{1}
			   ,{2})";

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

		public async Task Log(string message, LogLevel logLevel = LogLevel.Debug)
		{
			using SqlConnection connection = new(this._connectionString);
			connection.Insert(LOG_INSERT, (int)logLevel, message, DateTime.Now);
		}
	}
}