using LoggingApi.Interfaces;
using System.Text.Json.Serialization;

namespace LoggingApi.Services
{
    public class LogServiceSettings : IHasConnectionString
    {
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }
    }
}
