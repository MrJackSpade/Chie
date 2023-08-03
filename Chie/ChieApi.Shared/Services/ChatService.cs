using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Entities;
using Loxifi.Extensions;
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

        public float[]? GetEmbeddings(long chatEntryId)
        {
            using SqlConnection connection = new(this._connectionString);

            ChatEntryEmbedding? embeddings = connection.Query<ChatEntryEmbedding>($"select * from ChatEntryEmbedding where ChatEntryId = {chatEntryId}").SingleOrDefault();

            if (embeddings == null)
            {
                return null;
            }

            float[] floats = new float[embeddings.Data.Length / sizeof(float)];

            for (int i = 0; i < embeddings.Data.Length; i += sizeof(float))
            {
                floats[i / sizeof(float)] = BitConverter.ToSingle(embeddings.Data, i);
            }

            return floats;
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

            string query = $"select * from chatentry where 1 = 1";

            if (userId != null)
            {
                query += $" and UserId = '{userId}' ";
            }

            if (!includeHidden)
            {
                query += $" and IsVisible = 1 ";
            }

            query += $"  order by id desc";

            return connection.Query<ChatEntry>(query).Take(100).ToArray();
        }

        public ChatEntry GetBefore(long id, bool includeHidden = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select top 1 * from chatentry where id < {id}";

            if (!includeHidden)
            {
                query += $" and IsVisible = 1 ";
            }

            query += $"  order by id desc";

            return connection.Query<ChatEntry>(query).SingleOrDefault();
        }

        public ChatEntry[] GetMessages(string? channelId = null, long after = 0, string userId = null, bool includeHidden = false, bool includeTemporary = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from chatentry where id > {after}";

            if (!string.IsNullOrWhiteSpace(channelId))
            {
                query += $" and SourceChannel = '{channelId}' ";
            }

            if (userId != null)
            {
                query += $" and UserId = '{userId}' ";
            }

            if (!includeHidden)
            {
                query += $" and IsVisible = 1 ";
            }

            if (!includeTemporary)
            {
                query += $" and (Type is null OR Type != {(int)LlamaTokenType.Temporary}) ";
            }

            query += "order by id asc";

            ChatEntry[] chatEntries = connection.Query<ChatEntry>(query).ToArray();

            return chatEntries;
        }

        public ChatEntry[] GetMissingEmbeddings(bool includeHidden = false, bool includeTemporary = false)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = "select ce.* from ChatEntry ce left outer join ChatEntryEmbedding cee on cee.chatentryid = ce.id where cee.chatentryid is null";

            if (!includeHidden)
            {
                query += $" and IsVisible = 1 ";
            }

            if (!includeTemporary)
            {
                query += $" and (Type = 0 OR Type != {(int)LlamaTokenType.Temporary}) ";
            }

            ChatEntry[] ids = connection.Query<ChatEntry>(query).ToArray();

            return ids;
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

            string query = "select userid from chatentry where id in (select max(id) from ChatEntry group by userid) and len(userid) > 0 and IsVisible = 1 order by id desc";

            return connection.Query<ChatEntry>(query).Select(c => c.UserId).ToList();
        }

        public long Save(ChatEntry chatEntry)
        {
            chatEntry.DateCreated = DateTime.Now;

            using SqlConnection connection = new(this._connectionString);

            connection.Insert(chatEntry);

            return chatEntry.Id;
        }

        public void SaveEmbeddings(long id, float[] embeddings)
        {
            using SqlConnection connection = new(this._connectionString);

            ChatEntryEmbedding cee = new()
            {
                ChatEntryId = id,
                Data = new byte[embeddings.Length * sizeof(float)]
            };

            for (int i = 0; i < embeddings.Length; i++)
            {
                int targetIndex = i * sizeof(float);
                byte[] thisFloat = BitConverter.GetBytes(embeddings[i]);
                for (int j = 0; j < thisFloat.Length; j++)
                {
                    cee.Data[targetIndex + j] = thisFloat[j];
                }
            }

            connection.Insert(cee, commandTimeout: int.MaxValue);
        }

        public bool TryGetOriginal(long originalMessageId, out ChatEntry? chatEntry)
        {
            using SqlConnection connection = new(this._connectionString);

            chatEntry = connection.Query<ChatEntry>($"select * from chatentry where ReplyToId = {originalMessageId}").FirstOrDefault();

            return chatEntry != null;
        }
    }
}