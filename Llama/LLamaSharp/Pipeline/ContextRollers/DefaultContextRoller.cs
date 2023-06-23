using Llama.Collections;
using Llama.Context.Interfaces;
using System;

namespace Llama.Pipeline.ContextRollers
{
    public class DefaultContextRoller : IContextRoller
    {
        public LlamaTokenCollection GenerateContext(IContext context, LlamaTokenCollection originalPrompt, int keepTokens) => throw new NotImplementedException();
    }
}