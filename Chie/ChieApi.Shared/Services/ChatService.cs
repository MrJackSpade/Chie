using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Loxifi.Database.Extensions;
using System.Data.SqlClient;

namespace ChieApi.Shared.Services
{
    public class ChatService
    {
        private readonly string _connectionString;

        public ChatService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
        }

        public ChatEntry GetLastMessage(string userId = null, bool includeHidden = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = "select top 1 * from chatentry where 1 = 1";

            if (!string.IsNullOrEmpty(userId))
            {
                query += $" and UserId = '{userId}'";
            }

            if (!includeHidden)
            {
                query += " and isVisible = 1 ";
            }

            query += " order by id desc";

            return connection.Query<ChatEntry>(query).FirstOrDefault();
        }

        public IEnumerable<ChatEntry> GetLastMessages(string userId, bool includeHidden = false)
        {
            using SqlConnection connection = new(this._connectionString);
            string query = $"select * from chatentry ";

            if (userId != null)
            {
                query += $" where UserId = '{userId}' ";
            }

            query += $" and IsVisible = 1 order by id desc";

            return connection.Query<ChatEntry>(query).Take(100).ToArray();
        }

        public ChatEntry[] GetMessages(string channelId, long after, string userId = null, bool includeHidden = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from chatentry where id > {after} and SourceChannel = '{channelId}' ";

            if (userId != null)
            {
                query += $" and UserId = '{userId}' ";
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

        public IEnumerable<string> GetUserIds()
        {
            using SqlConnection connection = new(this._connectionString);

            string query = "select distinct UserId from ChatEntry where isvisible = 1";

            return connection.Query<ChatEntry>(query).Select(c => c.UserId).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        }

        public async Task<long> Save(ChatEntry chatEntry)
        {
            chatEntry.DateCreated = DateTime.Now;

            using SqlConnection connection = new(this._connectionString);

            return connection.Insert(chatEntry).Value;
        }

        public bool TryGetOriginal(long originalMessageId, out ChatEntry? chatEntry)
        {
            using SqlConnection connection = new(this._connectionString);

            chatEntry = connection.Query<ChatEntry>($"select * from chatentry where ReplyToId = {originalMessageId}").FirstOrDefault();

            return chatEntry != null;
        }
    }
}