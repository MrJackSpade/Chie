using System.Text.Json.Serialization;

namespace Blip.Client
{
	public class BlipApiClientSettings
	{
		[JsonPropertyName("rootUrl")]
		public string RootUrl { get; set; }
	}
}