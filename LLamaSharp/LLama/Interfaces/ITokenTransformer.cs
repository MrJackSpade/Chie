﻿using Llama;
using Llama.Models;
using Llama.Native;
using System.Collections.Generic;

namespace Llama.Interfaces
{
	public interface ITokenTransformer
	{
		IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, SafeLlamaContext context, IEnumerable<LlamaToken> selectedTokens);
	}
}
