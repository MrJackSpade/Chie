using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LlamaApi.Models
{
    public class Job
    {
        [Key]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("state")]
        public JobState State { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }
}
