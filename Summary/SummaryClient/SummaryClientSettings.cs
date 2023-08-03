using System.Text.Json.Serialization;

namespace ImageRecognition
{
    public class SummaryClientSettings
    {
        [JsonPropertyName("summaryPath")]
        public string SummaryPath { get; set; }

        [JsonPropertyName("pythonPath")]
        public string PythonPath { get; set; }

        [JsonPropertyName("tokenizePath")]
        public object TokenizePath { get; set; }
    }
}