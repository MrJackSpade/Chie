using Llama.Collections;

namespace Llama.Models
{
    public class ContextState
	{
		public int InsertAt { get; set; }

		public LlamaTokenCollection Tokens { get; set; } = new LlamaTokenCollection();
	}
}