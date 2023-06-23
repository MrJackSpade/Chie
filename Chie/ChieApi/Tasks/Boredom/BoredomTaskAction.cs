using System.Text.Json.Serialization;

namespace ChieApi.Tasks.Boredom
{
    public class BoredomTaskAction : TriggerPeriod
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}