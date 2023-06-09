using System.Text.Json.Serialization;

namespace ChromaDbClient.Models
{
	public class BaseResponse
	{
		[JsonPropertyName("detail")]
		public List<ValidationError> Details { get; set; } = new List<ValidationError>();

		[JsonPropertyName("error")]
		public string Error { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		public bool IsSuccess => string.IsNullOrWhiteSpace(this.Error) && !this.Details.Any();
	}
}
