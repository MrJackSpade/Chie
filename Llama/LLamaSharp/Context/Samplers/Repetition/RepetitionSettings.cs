namespace Llama.Context.Samplers.Repetition
{
    public class RepetitionSamplerSettings
    {
        public float RepeatPenalty { get; set; } = 1.10f;

        public int RepeatTokenPenaltyWindow { get; set; } = 64;
    }
}