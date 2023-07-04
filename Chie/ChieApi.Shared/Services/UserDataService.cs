using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Loxifi.Database.Extensions;
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

        public UserData? GetOrDefault(string userId)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from UserData where UserId = '{userId}'";
            string noUserQuery = $"select * from UserData where UserId = ''";
            return connection.Query<UserData>(query).FirstOrDefault() ?? connection.Query<UserData>(noUserQuery).FirstOrDefault();
        }

        public UserData? Get(string userId)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from UserData where UserId = '{userId}'";
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

                long id = await this.Save(userData);

                userData.Id = id;
            }

            return userData;
        }

        public async Task<long> Save(UserData userData)
        {
            using SqlConnection connection = new(this._connectionString);

            if (userData.Id != 0)
            {
                connection.Update(userData);
                return userData.Id;
            }
            else
            {
                return connection.Insert(userData).Value;
            }
        }
    }
}