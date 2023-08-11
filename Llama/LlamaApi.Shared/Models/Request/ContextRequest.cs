using Llama.Data;
using Llama.Data.Models.Settings;
using Llama.Data.Scheduler;
using LlamaApi.Attributes;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    [RequiresAny(nameof(MirostatSamplerSettings), nameof(TemperatureSamplerSettings))]
    public class ContextRequest
    {
        [JsonPropertyName("ComplexPresence")]
        public ComplexPresencePenaltySettings? ComplexPresencePenaltySettings { get; set; }

        [JsonPropertyName("contextId")]
        public Guid? ContextId { get; set; }

        [JsonPropertyName("frequencyAndPresence")]
        public FrequencyAndPresenceSamplerSettings? FrequencyAndPresenceSamplerSettings { get; set; }

        [JsonPropertyName("mirostat")]
        public MirostatSamplerSettings? MirostatSamplerSettings { get; set; }

        [JsonPropertyName("modelId")]
        public Guid ModelId { get; set; }

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }

        [JsonPropertyName("repetition")]
        public RepetitionSamplerSettings? RepetitionSamplerSettings { get; set; }

        [JsonPropertyName("settings")]
        public LlamaContextSettings Settings { get; set; } = new LlamaContextSettings();

        [JsonPropertyName("temperature")]
        public TemperatureSamplerSettings? TemperatureSamplerSettings { get; set; }
    }
}