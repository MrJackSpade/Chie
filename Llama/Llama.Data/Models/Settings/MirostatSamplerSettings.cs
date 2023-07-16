namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatSamplerSettings
    {
        /// <summary>
        /// 100
        /// </summary>
        public readonly int M = 100;

        /// <summary>
        /// Default 0.1
        /// </summary>
        public float Eta { get; set; } = 0.10f;

        /// <summary>
        /// Tau * 2
        /// </summary>
        public float InitialMu => this.Tau * 2.0f;

        /// <summary>
        /// Default 5
        /// </summary>
        public float Tau { get; set; } = 5.00f;

        /// <summary>
        /// Default 0.85
        /// </summary>
        public float Temperature { get; set; } = 0.85f;
    }
}