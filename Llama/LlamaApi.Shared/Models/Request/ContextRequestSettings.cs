using Llama.Core.Samplers.Mirostat;
using Llama.Data.Models.Settings;
using MirostatSamplerSettings = LlamaApi.Models.Request.MirostatSamplerSettings;

namespace LlamaApi.Shared.Models.Request
{
    public class ContextRequestSettings
    {
        public ComplexPresencePenaltySettings ComplexPresencePenaltySettings { get; set; } = new();

        public MirostatSamplerSettings MirostatSamplerSettings { get; set; } = new();

        public MirostatTempSamplerSettings MirostatTempSamplerSettings { get; set; } = new();

        public RepetitionSamplerSettings RepetitionSamplerSettings { get; set; } = new();

        public TemperatureSamplerSettings TemperatureSamplerSettings { get; set; } = new();
    }
}