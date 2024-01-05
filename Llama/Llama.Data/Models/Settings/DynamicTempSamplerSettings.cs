namespace Llama.Core.Samplers.Mirostat
{
    public class DynamicTempSamplerSettings
    {
        /// <summary>
        /// If true, Mirostat will use preserved words to adjust the temperature.
        /// If false, only words selected by temperature will be used to guide
        /// algorithm
        /// </summary>
        public bool FactorPreservedWords { get; set; } = false;

        /// <summary>
        /// Default 0.1
        /// </summary>
        public float LearningRate { get; set; } = 0.25f;

        /// <summary>
        /// 
        /// </summary>
        public float MaxTarget { get; set; } = 1f;

        /// <summary>
        /// 
        /// </summary>
        public float MinTarget { get; set; } = 0f;

        /// <summary>
        /// Minimum value that will allow a return for the EOS token
        /// </summary>
        public Dictionary<int, float>? MinPs { get; set; }

        /// <summary>
        /// Min probability across all tokens
        /// </summary>
        public float MinP { get; set; } = 0.03f;

        /// <summary>
        /// Default 40
        /// </summary>
        public float Penalty { get; set; } = -2f;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public bool PreserveWords { get; set; } = true;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public float Scale { get; set; } = 1f;

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