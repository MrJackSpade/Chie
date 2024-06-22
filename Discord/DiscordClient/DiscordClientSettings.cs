using System.Text.Json.Serialization;

namespace Chie
{
    public class DiscordClientSettings
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }
}