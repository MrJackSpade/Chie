using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class JobStateRequest
    {
        [JsonPropertyName("jobId")]
        public ulong JobId { get; set; }
    }
}
