namespace Llama.Context.Samplers.Mirostat
{
    public class MirostatSamplerSettings
    {
        public readonly int M = 100;

        public float Eta { get; set; } = 0.10f;

        public float InitialMu => this.Tau * 2.0f;

        public float Tau { get; set; } = 5.00f;

        public float Temperature { get; set; } = 0.85f;
    }
}