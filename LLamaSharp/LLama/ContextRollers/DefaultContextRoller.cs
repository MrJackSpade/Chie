using Llama.Collections;
using Llama.Interfaces;
using Llama.Native;
using System;

namespace Llama.ContextRollers
{
    public class DefaultContextRoller : IContextRoller
	{
		public LlamaTokenCollection GenerateContext(SafeLlamaContext context, LlamaTokenCollection queue, LlamaTokenCollection originalPrompt, int keepTokens) => throw new NotImplementedException();
	}
}