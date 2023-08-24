using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Loxifi.Extensions;
using System.Data.SqlClient;

namespace ChieApi.Shared.Services
{
    public class UserDataService
    {
        private readonly string _connectionString;

        public UserDataService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
        }

        public void Encounter(string userId)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"update UserData set LastEncountered = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' where UserId = '{userId}'";

            connection.Execute(query);
        }

        public UserData? Get(string userId)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from UserData where UserId = '{userId}'";
            return connection.Query<UserData>(query).FirstOrDefault();
        }

        public UserData? GetByDisplayName(string displayName)
        {
            using SqlConnection connection = new(this._connectionString);
            //TODO: FixMe
            string query = $"select * from UserData where DisplayName = '{displayName}' or (DisplayName is null and userid = '{displayName}')";
            return connection.Query<UserData>(query).FirstOrDefault();
        }

        public async Task<UserData> GetOrCreate(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException($"'{nameof(userId)}' cannot be null or whitespace.", nameof(userId));
            }

            UserData? userData = this.Get(userId);

            if (userData is null)
            {
                userData = new UserData()
                {
                    UserId = userId,
                };

                long id = this.Save(userData);

                userData.Id = id;
            }

            return userData;
        }

        public UserData? GetOrDefault(string userId)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from UserData where UserId = '{userId}'";
            string noUserQuery = $"select * from UserData where UserId = ''";
            return connection.Query<UserData>(query).FirstOrDefault() ?? connection.Query<UserData>(noUserQuery).FirstOrDefault();
        }

        public long Save(UserData userData)
        {
            using SqlConnection connection = new(this._connectionString);

            if (userData.Id != 0)
            {
                connection.Update(userData);
            }
            else
            {
                connection.Insert(userData);
            }

            return userData.Id;
        }
    }
}