namespace Llama.Core.Samplers.Mirostat
{
    public class MirostatTempSamplerSettings
    {
        /// <summary>
        /// 100
        /// </summary>
        public readonly int M = 100;

        /// <summary>
        /// If true, Mirostat will use preserved words to adjust the temperature.
        /// If false, only words selected by temperature will be used to guide
        /// algorithm
        /// </summary>
        public bool FactorPreservedWords { get; set; } = false;

        /// <summary>
        /// Tau * 2
        /// </summary>
        public float InitialMu => this.Target * 2.0f;

        /// <summary>
        /// Default 40
        /// </summary>
        public float InitialTemperature { get; set; } = 0.75f;

        /// <summary>
        /// Default 0.1
        /// </summary>
        public float LearningRate { get; set; } = 0.25f;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public bool PreserveWords { get; set; } = true;

        /// <summary>
        /// Default 5
        /// </summary>
        public float Target { get; set; } = 5.00f;

        /// <summary>
        /// Default 40
        /// </summary>
        public float TemperatureLearningRate { get; set; } = 0.05f;

        /// <summary>
        /// Default 0.95
        /// </summary>
        public float Tfs { get; set; } = 0.95f;

        /// <summary>
        /// Max Temp
        /// </summary>
        public float MaxTemp { get; set; } = 1.25f;
    }
}