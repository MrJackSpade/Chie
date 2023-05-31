using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChieApi.Shared.Entities
{
	[Table("ChatEntries")]
	public record ChatEntry
	{
		[JsonPropertyName("isVisible")]
		public bool IsVisible { get; set; } = true;

		[JsonPropertyName("sourceChannel")]
		public string SourceChannel { get; set; }

		[JsonPropertyName("content")]
		public string Content { get; set; } = string.Empty;

		[JsonPropertyName("dateCreated")]
		public DateTime DateCreated { get; set; }

		public bool HasImage => this.Image is not null && this.Image.Length > 0;

		public bool HasText => !string.IsNullOrWhiteSpace(this.Content);

		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("image")]
		public byte[] Image { get; set; } = Array.Empty<byte>();

		[JsonPropertyName("replyToId")]
		public long ReplyToId { get; set; }

		[JsonPropertyName("sourceUser")]
		public string? SourceUser { get; set; }
	}
}