using LLama.Interfaces;
using LLama.Models;
using LLama.Native;
using System;

namespace LLama.ContextRollers
{
	public class DefaultContextRoller : IContextRoller
	{
		public ContextState GenerateContext(SafeLLamaContext context, LlamaTokenCollection originalPrompt, LlamaTokenCollection history, int keepTokens) => throw new NotImplementedException();
	}
}