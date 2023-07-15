using LlamaApi.Interfaces;
using System.Text.Json.Serialization;

namespace LlamaApi
{
    public class DatabaseConnectionSettings : IHasConnectionString
    {
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }
    }
}