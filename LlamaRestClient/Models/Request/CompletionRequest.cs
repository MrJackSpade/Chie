using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Llama.Models.Request
{
	public class CompletionRequest
	{
		[JsonPropertyName("prompt")]
		public string? Prompt { get; set; }

		[JsonPropertyName("batch_size")]
		public int BatchSize { get; set; } = 128;

		[JsonPropertyName("temperature")]
		public float Temperature { get; set; } = .75f;

		[JsonPropertyName("top_k")]
		public int TopK { get; set; } = 40;

		[JsonPropertyName("top_p")]
		public float TopP { get; set; } = .9f;

		[JsonPropertyName("n_keep")]
		public int NKeep { get; set; } = -1;

		[JsonPropertyName("n_predict")]
		public int NPredict { get; set; } = -1;

		[JsonPropertyName("stop")]
		public List<string> Stop { get; set; } = new List<string>();

		[JsonPropertyName("exclude")]
		public List<string> Exclude { get; set; } = new List<string>();

		[JsonPropertyName("threads")]
		public int Threads { get; set; }

		[JsonPropertyName("as_loop")]
		public bool AsLoop => _asLoop;

		[SuppressMessage("Style", "IDE0032:Use auto property")]
		private bool _asLoop;

		internal void SetAsLoop(bool value) => this._asLoop = value;

		[JsonPropertyName("interactive")]
		public bool Interactive => this.Stop.Any();
	}
}