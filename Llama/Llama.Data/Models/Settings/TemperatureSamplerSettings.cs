namespace Llama.Data.Models.Settings
{
    public class TemperatureSamplerSettings
    {
        public float Temperature { get; set; } = 0.80f;

        public float TfsZ { get; set; } = 1.00f;

        public int TopK { get; set; } = 40;

        public float TopP { get; set; } = 0.95f;

        public float TypicalP { get; set; } = 1.00f;
    }
}