using Llama.Collections;
using Llama.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Llama.Interfaces
{
    public interface IReadOnlyLlamaTokenCollection : IEnumerable<LlamaToken>
	{
		LlamaToken this[int index] { get; }

		int Count { get; }
		IEnumerable<int> Ids { get; }

		LlamaTokenCollection From(int startIndex, LlamaToken startToken);
		LlamaTokenCollection Replace(LlamaTokenCollection toFind, LlamaTokenCollection toReplace);
	}
}
