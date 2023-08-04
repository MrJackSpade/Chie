using System.Text.Json.Serialization;

namespace ImageRecognition
{
    public class EmbeddingClientSettings
    {
        [JsonPropertyName("EmbeddingPath")]
        public string EmbeddingPath { get; set; }

        [JsonPropertyName("pythonPath")]
        public string PythonPath { get; set; }

        [JsonPropertyName("modelName")]
        public string ModelName { get; set; }
    }
}