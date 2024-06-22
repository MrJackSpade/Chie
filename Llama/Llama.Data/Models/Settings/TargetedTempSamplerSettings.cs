namespace Llama.Data.Models.Settings
{
    public class TargetedTempSamplerSettings : BaseDynamicSamplerSettings
    {
        /// <summary>
        /// If true, Mirostat will use preserved words to adjust the temperature.
        /// If false, only words selected by temperature will be used to guide
        /// algorithm
        /// </summary>
        public bool FactorPreservedWords { get; set; } = false;

        /// <summary>
        ///
        /// </summary>
        public float MaxTarget { get; set; } = 1f;

        /// <summary>
        /// Min probability across all tokens
        /// </summary>
        public float MinP { get; set; } = 0.05f;

		/// <summary>
		///
		/// </summary>
		public float MinTarget { get; set; } = 0f;

		/// <summary>
		/// The min probability for any word continuation
		/// </summary>
		public float PreserveWordMinP { get; set; } = .2f;

		/// <summary>
		/// The certainty at which word continuations are greedy sampled
		/// </summary>
		public float PreserveWordMaxP { get; set; } = .8f;

		/// <summary>
		/// Size of the token queue for dynamic adjustment
		/// </summary>
		public int QueueSize { get; set; } = 10;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public float Scale { get; set; } = 1f;

        /// <summary>
        /// Default .4
        /// </summary>
        public float Target { get; set; } = 0.4f;

        /// <summary>
        /// Default 40
        /// </summary>
        public float Temperature { get; set; } = 1.2f;

        /// <summary>
        /// Default 0.95
        /// </summary>
        public float Tfs { get; set; } = 0.95f;
    }
}