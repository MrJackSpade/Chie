using ChieApi.Interfaces;
using Llama.Shared;
using System.Text.Json.Serialization;

namespace ChieApi
{
    public class UserSummarizerSettings : LlamaSettings, IHasConnectionString
    {
        [JsonPropertyName("characterName")]
        public string CharacterName { get; set; }

        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; }
    }
}