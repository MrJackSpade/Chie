using Llama.Core.Samplers.Mirostat;
using Llama.Data.Models.Settings;
using System.Text.Json.Serialization;
using MirostatSamplerSettings = LlamaApi.Models.Request.MirostatSamplerSettings;

namespace LlamaApi.Shared.Models.Request
{
    public class ContextRequestSettings
    {
        [JsonPropertyName("ComplexPresence")]
        public ComplexPresencePenaltySettings? ComplexPresencePenaltySettings { get; set; }

        [JsonPropertyName("mirostat")]
        public MirostatSamplerSettings? MirostatSamplerSettings { get; set; }

        [JsonPropertyName("mirostatTemp")]
        public MirostatTempSamplerSettings? MirostatTempSamplerSettings { get; set; }

        [JsonPropertyName("repetition")]
        public RepetitionSamplerSettings? RepetitionSamplerSettings { get; set; }

        [JsonPropertyName("temperature")]
        public TemperatureSamplerSettings? TemperatureSamplerSettings { get; set; }
    }
}