using Llama.Collections;
using Llama.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Llama.Interfaces
{
    public interface ILlamaTokenCollection : IReadOnlyLlamaTokenCollection
	{
		void Append(LlamaToken token);
		void AppendControl(int id);
		void Clear();
		LlamaTokenCollection Replace(LlamaTokenCollection toFind, LlamaTokenCollection toReplace);
	}
}
