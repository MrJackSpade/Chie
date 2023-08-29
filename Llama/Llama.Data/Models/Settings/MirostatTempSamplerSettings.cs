namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatTempSamplerSettings
    {
        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public bool PreserveWords { get; set; } = true;

		/// <summary>
		/// If true, Mirostat will use preserved words to adjust the temperature.
        /// If false, only words selected by temperature will be used to guide 
        /// algorithm
		/// </summary>
		public bool FactorPreservedWords { get; set; } = false;

		/// <summary>
		/// 100
		/// </summary>
		public readonly int M = 100;

        /// <summary>
        /// Default 0.1
        /// </summary>
        public float LearningRate { get; set; } = 0.10f;

        /// <summary>
        /// Tau * 2
        /// </summary>
        public float InitialMu => this.Target * 2.0f;

        /// <summary>
        /// Default 5
        /// </summary>
        public float Target { get; set; } = 5.00f;

        /// <summary>
        /// Default 40
        /// </summary>
        public int TopK { get; set; } = 40;

		/// <summary>
		/// Default 0.01
		/// </summary>
		public float MinP { get; set; } = 0.1f;

		/// <summary>
		/// Default 40
		/// </summary>
		public float InitialTemperature { get; set; } = 0.75f;

		/// <summary>
		/// Default 40
		/// </summary>
		public float TemperatureLearningRate { get; set; } = 0.02f;
	}
}