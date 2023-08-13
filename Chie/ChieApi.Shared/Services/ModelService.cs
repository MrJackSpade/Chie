using ChieApi.Interfaces;
using ChieApi.Shared.Models;
using Loxifi.Extensions;
using System.Data.SqlClient;

namespace ChieApi.Shared.Services
{
    public class ModelService
    {
        private readonly string _connectionString;

        public ModelService(IHasConnectionString connectionString)
        {
            this._connectionString = connectionString.ConnectionString;
        }

        public Model GetModel(string name)
        {
            using SqlConnection connection = new(this._connectionString);

            Model? model = connection.Query<Model>($"select * from Model where Name = '{name}'").SingleOrDefault();

            return model;
        }
    }
}