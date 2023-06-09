using LLama.Models;
using LLama.Native;

namespace LLama.Interfaces
{
	public interface IContextRoller
	{
		public ContextState GenerateContext(SafeLLamaContext context, LlamaTokenCollection originalPrompt, LlamaTokenCollection history, int keepTokens);
	}
}