using Llama.Collections;

namespace Llama.Context.Interfaces
{
    public interface IContextRoller
    {
        public LlamaTokenCollection GenerateContext(IContext context, LlamaTokenCollection originalPrompt, int keepTokens);
    }
}