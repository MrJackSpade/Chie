namespace Llama.Data.Models.Settings
{
    public class ComplexPresencePenaltySettings
    {
        /// <summary>
        /// Default 1
        /// </summary>
        public float GroupScale { get; set; } = 1f;

        /// <summary>
        /// Default 1
        /// </summary>
        public float LengthScale { get; set; } = 1f;

        /// <summary>
        /// Default 0
        /// </summary>
        public int MinGroupLength { get; set; } = 0;

        /// <summary>
        /// Default 64
        /// </summary>
        public int RepeatTokenPenaltyWindow { get; set; } = 64;
    }
}