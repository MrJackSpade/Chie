using ChieApi.Interfaces;
using System.Text.Json.Serialization;

namespace UserSummarizer
{
    public class UserSummarizerSettings: IHasConnectionString
    {
        [JsonPropertyName("characterName")]
        public string CharacterName { get; set; }

        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; }
    }
}