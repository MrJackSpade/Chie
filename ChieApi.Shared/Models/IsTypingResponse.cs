using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
	public class IsTypingResponse
	{
		[JsonPropertyName("isTyping")]
		public bool IsTyping { get; set; }
	}
}