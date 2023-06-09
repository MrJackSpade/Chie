using LLama;
using LLama.Models;
using LLama.Native;
using System.Collections.Generic;

namespace LLama.Interfaces
{
	public interface ITokenTransformer
	{
		IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, SafeLLamaContext context, IEnumerable<LlamaToken> selectedTokens);
	}
}
