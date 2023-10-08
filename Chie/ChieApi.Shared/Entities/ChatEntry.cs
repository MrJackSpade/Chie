using ChieApi.Models;
using System.ComponentModel.DataAnnotations;
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

		[NotMapped]
		public bool HasImage => this.Image is not null && this.Image.Length > 0;

		[NotMapped]
		public bool HasText => !string.IsNullOrWhiteSpace(this.Content);

		[Key]
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[NotMapped]
		[JsonPropertyName("image")]
		public byte[] Image { get; set; } = Array.Empty<byte>();

		[JsonPropertyName("replyToId")]
		public long ReplyToId { get; set; }

		[JsonPropertyName("displayName")]
		public string? DisplayName { get; set; }

		private string _id;

		[JsonPropertyName("userId")]
		public string? UserId
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this._id))
				{
					return this.DisplayName;
				}

				return this._id;
			}
			set => this._id = value;
		}

		[JsonPropertyName("type")]
		public LlamaTokenType Type { get; set; }
	}
}