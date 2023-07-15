using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Models
{
    public class Job
    {
        [Key]
        public long Id { get; set; }

        public JobState State { get; set; }

        public string? Result { get; set; }
    }
}
