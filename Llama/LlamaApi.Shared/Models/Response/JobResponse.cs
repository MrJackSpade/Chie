using System.Text.Json.Nodes;

namespace LlamaApi.Models.Response
{
    public class JobResponse
    {
        public long Id { get; set; }

        public JobState State { get; set; }

        public JsonNode? Result { get; set; }
    }
}
