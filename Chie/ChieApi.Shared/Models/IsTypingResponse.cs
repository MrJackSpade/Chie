using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
	public class IsTypingResponse
	{
		[JsonPropertyName("content")]
		public string Content { get; set; }

		[JsonPropertyName("isTyping")]
		public bool IsTyping { get; set; }

		[JsonPropertyName("replyTo")]
		public int ReplyTo { get; set; }
	}
}