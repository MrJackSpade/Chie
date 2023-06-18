using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Dapper;
using System.Data.SqlClient;

namespace ChieApi.Services
{
    public class UserDataService
    {
        private const string USER_DATA_INSERT = @"
		INSERT INTO [dbo].[UserData]
           ([UserName]
           ,[UserPrompt]
           ,[UserSummary])
		VALUES
		   ({0}
		   ,{1}
		   ,{2})
		";

        private readonly string _connectionString;

        private readonly UserData _defaultUserData;

        public UserDataService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
            this._defaultUserData = this.GetUserData("");
        }

        public UserData GetUserData(string userName)
        {
            using SqlConnection connection = new(this._connectionString);

            string query = $"select * from UserData where UserName = '{userName}'";

            return connection.Query<UserData>(query).FirstOrDefault() ?? this._defaultUserData;
        }

        public async Task<long> Save(UserData userData)
        {
            using SqlConnection connection = new(this._connectionString);

            return connection.Insert(USER_DATA_INSERT, userData.UserName, userData.UserPrompt, userData.UserSummary);
        }
    }
}