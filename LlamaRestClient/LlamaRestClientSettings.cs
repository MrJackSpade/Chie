using Loxifi.Attributes;

namespace Llama
{
	public class LlamaRestClientSettings
	{
		public string? ServerExePath { get; set; }

		[Parameter("--ctx_size")]
		public int ContextSize { get; set; } = 2048;

		[Parameter("-m")]
		public string? ModelPath { get; set; }

		[FlagParameter("--embedding")]
		public bool EmbeddingOnly { get; set; }

		[Parameter("--host", false)]
		public string? Host { get; set; } = "localhost";

		[Parameter("--port", false)]
		public int Port { get; set; } = 8080;

		[Parameter("--seed", false)]
		public int Seed { get; set; }

		[FlagParameter("--no-mmap")]
		public bool DisableMemoryMap { get; set; } = true;

		[FlagParameter("--mlock")]
		public bool LockModelInMemory { get; set; } = true;

		[FlagParameter("--memory_f32")]
		public bool Float32 { get; set; } = true;
	}
}