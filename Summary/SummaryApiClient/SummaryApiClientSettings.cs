using System.Text.Json.Serialization;

namespace Summary
{
    public class SummaryApiClientSettings
    {
        [JsonPropertyName("rootUrl")]
        public string RootUrl { get; set; }
    }
}