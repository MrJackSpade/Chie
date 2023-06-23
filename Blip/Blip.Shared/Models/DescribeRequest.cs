using System.Text.Json.Serialization;

namespace Blip.Models
{
    public class DescribeRequest
    {
        [JsonPropertyName("fileData")]
        public byte[]? FileData { get; set; }

        [JsonPropertyName("filePath")]
        public string? FilePath { get; set; }
    }
}