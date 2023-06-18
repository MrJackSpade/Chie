using System.Text.Json.Serialization;

namespace ChieApi.Tasks.Boredom
{
    public class BoredomTaskSettings
    {
        [JsonPropertyName("probability")]
        public float Probability { get; set; } = .015f;

        [JsonPropertyName("taskActions")]
        public BoredomTaskAction[] TaskActions { get; set; } = Array.Empty<BoredomTaskAction>();
    }
}