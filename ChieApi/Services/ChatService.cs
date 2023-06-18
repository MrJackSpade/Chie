using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Dapper;
using System.Data.SqlClient;

namespace ChieApi.Services
{
    public class ChatService
    {
        private const string CHAT_INSERT = @"INSERT INTO [dbo].[ChatEntry]
           ([DateCreated]
           ,[SourceUser]
           ,[Content]
           ,[ReplyToId]
		   ,[SourceChannel]
		   ,[IsVisible]
		   ,[Tag]
		   )
			output INSERTED.ID
			 VALUES
				   ({0}
				   ,{1}
				   ,{2}
				   ,{3}
				   ,{4}
				   ,{5}
				   ,{6}
			)";

        private readonly string _connectionString;

        public ChatService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
        }

        public ChatEntry GetLastMessage(string sourceUser = null, bool includeHidden = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = "select top 1 * from chatentry where 1 = 1";

            if (!string.IsNullOrEmpty(sourceUser))
            {
                query += " and SourceUser = " + sourceUser;
            }

            if (!includeHidden)
            {
                query += " and isVisible = 1 ";
            }

            query += " order by id desc";

            return connection.Query<ChatEntry>(query).FirstOrDefault();
        }

        public ChatEntry[] GetMessages(string channelId, long after, string username = null, bool includeHidden = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from chatentry where id > {after} and SourceChannel = '{channelId}' ";

            if (username != null)
            {
                query += $" and sourceUser = '{username}' ";
            }

            if (!includeHidden)
            {
                query += $" and IsVisible = 1 ";
            }

            query += "order by id asc";

            ChatEntry[] chatEntries = connection.Query<ChatEntry>(query).ToArray();

            return chatEntries;
        }

        public IEnumerable<ChatEntry> GetRecentMessages()
        {
            using SqlConnection connection = new(this._connectionString);

            string query = "select * from chatentry order by id desc";

            return connection.Query<ChatEntry>(query);
        }

        public async Task<long> Save(ChatEntry chatEntry)
        {
            chatEntry.DateCreated = DateTime.Now;

            using SqlConnection connection = new(this._connectionString);

            return connection.Insert(CHAT_INSERT, chatEntry.DateCreated, chatEntry.SourceUser, chatEntry.Content, chatEntry.ReplyToId, chatEntry.SourceChannel, chatEntry.IsVisible, chatEntry.Tag);
        }

        public bool TryGetOriginal(long originalMessageId, out ChatEntry? chatEntry)
        {
            using SqlConnection connection = new(this._connectionString);

            chatEntry = connection.Query<ChatEntry>($"select * from chatentry where ReplyToId = {originalMessageId}").FirstOrDefault();

            return chatEntry != null;
        }
    }
}