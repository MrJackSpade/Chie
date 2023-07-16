namespace Llama.Data.Models.Settings
{
    public class FrequencyAndPresenceSamplerSettings
    {
        /// <summary>
        /// Default 0
        /// </summary>
        public float FrequencyPenalty { get; set; } = 0.00f;

        /// <summary>
        /// Default 0
        /// </summary>
        public float PresencePenalty { get; set; } = 0.00f;

        /// <summary>
        /// Default 64
        /// </summary>
        public int RepeatTokenPenaltyWindow { get; set; } = 64;
    }
}