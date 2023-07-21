using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class PredictResponse
    {
        [JsonPropertyName("predicted")]
        public ResponseLlamaToken Predicted { get; set; }
    }
}