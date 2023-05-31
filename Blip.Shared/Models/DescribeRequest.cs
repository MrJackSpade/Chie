using System.Text.Json.Serialization;

namespace Blip.Shared.Models
{
	public class DescribeRequest
	{
		[JsonPropertyName("fileData")]
		public byte[]? FileData { get; set; }

		[JsonPropertyName("filePath")]
		public string? FilePath { get; set; }
	}
}