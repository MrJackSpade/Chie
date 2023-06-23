using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Loxifi.Database.Extensions;
using System.Data.SqlClient;

namespace ChieApi.Services
{
    public class UserDataService
    {
        private readonly string _connectionString;

        public UserDataService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
        }

        public UserData? GetUserData(string userName)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from UserData where UserName = '{userName}'";
            string noUserQuery = $"select * from UserData where UserName = ''";
            return connection.Query<UserData>(query).FirstOrDefault() ?? connection.Query<UserData>(noUserQuery).FirstOrDefault();
        }

        public async Task<long> Save(UserData userData)
        {
            using SqlConnection connection = new(this._connectionString);

            return connection.Insert(userData);
        }
    }
}