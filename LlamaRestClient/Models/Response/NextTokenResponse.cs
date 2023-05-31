using System.Text.Json.Serialization;

namespace Llama.Models.Response
{
	public class NextTokenResponse
	{
		[JsonPropertyName("stop")]
		public bool Stop { get; set; }

		[JsonPropertyName("content")]
		public string? Content { get; set; }
	}
}