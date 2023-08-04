using ChieApi.Interfaces;
using System.Text.Json.Serialization;

namespace ChatVectorizer
{
    public class ChatVectorizerSettings : IHasConnectionString
    {
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; }
    }
}