using Llama.Collections;
using Llama.Native;

namespace Llama.Interfaces
{
    public interface IContextRoller
	{
		public LlamaTokenCollection GenerateContext(SafeLlamaContext context, LlamaTokenCollection queue, LlamaTokenCollection originalPrompt, int keepTokens);
	}
}