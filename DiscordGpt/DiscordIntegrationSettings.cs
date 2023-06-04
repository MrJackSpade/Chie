using System.Text.Json.Serialization;

namespace DiscordGpt
{
	public class DiscordIntegrationSettings
	{
		[JsonPropertyName("allowDms")]
		public bool AllowDms { get; set; }

		[JsonPropertyName("useServerEmots")]
		public bool UseServerEmotes { get; set; }

		[JsonPropertyName("publicChannels")]
		public List<ulong> PublicChannels { get; set; } = new List<ulong>();
	}
}