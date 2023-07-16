namespace Llama.Core.Samplers.Repetition
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
    }
}