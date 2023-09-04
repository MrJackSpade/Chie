using ChieApi.Interfaces;
using System.Text.Json.Serialization;

namespace UserSummarizer
{
    public class ChatSummarizerSettings: IHasConnectionString
    {
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; }
    }
}