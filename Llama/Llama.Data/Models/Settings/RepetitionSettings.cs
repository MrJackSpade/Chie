namespace Llama.Data.Models.Settings
{
    public class RepetitionSamplerSettings
    {
        /// <summary>
        /// Default 1.1
        /// </summary>
        public float RepeatPenalty { get; set; } = 1.10f;

        /// <summary>
        /// Default 64
        /// </summary>
        public int RepeatTokenPenaltyWindow { get; set; } = 64;

        /// <summary>
        /// Default 0
        /// </summary>
        public float FrequencyPenalty { get; set; } = 0.00f;

        /// <summary>
        /// Default 0
        /// </summary>
        public float PresencePenalty { get; set; } = 0.00f;
    }
}