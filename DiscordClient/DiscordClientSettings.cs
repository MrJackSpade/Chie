using System.Text.Json.Serialization;

namespace Chie
{
	public class DiscordClientSettings
	{
		[JsonPropertyName("applicationId")]
		public string ApplicationId { get; set; }

		[JsonPropertyName("token")]
		public string Token { get; set; }
	}
}