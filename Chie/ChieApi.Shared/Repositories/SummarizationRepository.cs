using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Loxifi.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChieApi.Shared.Repositories
{
	public class SummarizationRepository
	{
		private readonly string _connectionString;

		public SummarizationRepository(IHasConnectionString connectionString)
		{
			this._connectionString = connectionString.ConnectionString;
		}

		public long GetLastTokenizedChat(int modelId)
		{
			using SqlConnection connection = new(this._connectionString);

			return connection.Query<long>($"select max(ChatEntryId) from TokenCount where modelid = {modelId}").Single();
		}

		public TokenCount? GetTokenCount(int modelId, int chatEntryId)
		{
			using SqlConnection connection = new(this._connectionString);

			return connection.Query<TokenCount>($"select * from TokenCount where modelid = {modelId} and chatentryid = {chatEntryId}").SingleOrDefault();
		}

		public void Add(TokenCount tokenCount)
		{
			using SqlConnection connection = new(this._connectionString);

			connection.Insert(tokenCount);
		}
	}
}
