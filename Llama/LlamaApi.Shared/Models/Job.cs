using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LlamaApi.Models
{
    public class Job
    {
        public Job() { 
        }

        [Key]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("state")]
        public JobState State { get; set; }

        [JsonPropertyName("machine")]
        public string? Machine { get; set; }

        [JsonPropertyName("caller")]
        public string? Caller { get; set; }
    }
}