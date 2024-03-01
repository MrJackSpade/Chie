namespace Llama.Data.Models.Settings
{
    public class TemperatureSamplerSettings
    {
        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public bool PreserveWords { get; set; } = true;

        public float Temperature { get; set; } = 1.0f;
    }
}