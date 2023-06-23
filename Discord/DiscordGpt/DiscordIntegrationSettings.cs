using System.Text.Json.Serialization;

namespace DiscordGpt
{
    public class DiscordIntegrationSettings
    {
        [JsonPropertyName("adminUser")]
        public string? AdminUser { get; set; }

        [JsonPropertyName("allowDms")]
        public bool AllowDms { get; set; }

        [JsonPropertyName("publicChannels")]
        public List<ulong> PublicChannels { get; set; } = new List<ulong>();

        [JsonPropertyName("useServerEmots")]
        public bool UseServerEmotes { get; set; }
    }
}