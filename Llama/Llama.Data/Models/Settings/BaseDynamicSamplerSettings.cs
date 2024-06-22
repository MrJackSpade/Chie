namespace Llama.Data.Models.Settings
{
    public class BaseDynamicSamplerSettings
    {
        /// <summary>
        /// Exclude specific tokens from greedy sampling
        /// </summary>
        public int[] GreedyExclude { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Include specific tokens in greedy sampling
        /// </summary>
        public int[] GreedyInclude { get; set; } = Array.Empty<int>();

		/// <summary>
		/// Minimum value that will allow a return for the EOS token
		/// </summary>
		public Dictionary<int, float> MinPs { get; set; } = new Dictionary<int, float>();

		/// <summary>
		/// Maximum value before token is greedy sampled
		/// </summary>
		public Dictionary<int, float> MaxPs { get; set; } = new Dictionary<int, float>();
	}
}
