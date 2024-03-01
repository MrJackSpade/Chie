namespace Llama.Core.Samplers.Mirostat
{
    public class TargetedTempSamplerSettings
    {
        /// <summary>
        /// If true, Mirostat will use preserved words to adjust the temperature.
        /// If false, only words selected by temperature will be used to guide
        /// algorithm
        /// </summary>
        public bool FactorPreservedWords { get; set; } = false;

        /// <summary>
        /// Exclude specific tokens from greedy sampling
        /// </summary>
        public int[] GreedyExclude { get; set; } = Array.Empty<int>();

        /// <summary>
        ///
        /// </summary>
        public float MaxTarget { get; set; } = 1f;

        /// <summary>
        /// Min probability across all tokens
        /// </summary>
        public float MinP { get; set; } = 0.03f;

        /// <summary>
        /// Minimum value that will allow a return for the EOS token
        /// </summary>
        public Dictionary<int, float> MinPs { get; set; } = new Dictionary<int, float>();

        /// <summary>
        ///
        /// </summary>
        public float MinTarget { get; set; } = 0f;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public bool PreserveWords { get; set; } = true;

        public int QueueSize { get; set; } = 3;

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