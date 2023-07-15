namespace Llama.Data.Models.Settings
{
    public class FrequencyAndPresenceSamplerSettings
    {
        public float FrequencyPenalty { get; set; } = 0.00f;

        public float PresencePenalty { get; set; } = 0.00f;

        public int RepeatTokenPenaltyWindow { get; set; } = 64;
    }
}