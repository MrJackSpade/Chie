using LLama.Exceptions;
using LLama.Models;
using LLama.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LLama.Native
{
	public class SafeLLamaContext
		: SafeLLamaHandleBase
	{
		private readonly Encoding _encoding;

		private readonly int _evalThreadCount;

		public SafeLLamaContext(IntPtr handle, Encoding encoding, int evalThreadCount, int size)
			: base(handle)
		{
			this._encoding = encoding;
			this._evalThreadCount = evalThreadCount;
			this.Size = size;
		}

		protected SafeLLamaContext()
		{
		}

		public int Size { get; private set; }

		public void Evaluate(IEnumerable<LlamaToken> collection, int n_eval, int _contextIndex)
		{
			int[] array = collection.Select(t => t.Id).ToArray();
			if (NativeApi.llama_eval(this, array, n_eval, _contextIndex, this._evalThreadCount) != 0)
			{
				LLamaLogger.Default.Error($"Failed to eval.");
				throw new RuntimeError("Failed to eval.");
			}
		}

		public LlamaToken GetToken(int id)
		{
			string v = Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this, id));

			return new LlamaToken(id, v);
		}

		public LlamaTokenCollection Tokenize(string value, bool addBos = false)
		{
			LlamaTokenCollection tokens = new();

			foreach (int id in Utils.llama_tokenize(this, value, addBos, this._encoding))
			{
				tokens.Append(this.GetToken(id));
			}

			return tokens;
		}

		public LlamaTokenCollection Tokenize(IEnumerable<int> value)
		{
			LlamaTokenCollection tokens = new();

			foreach (int id in value)
			{
				tokens.Append(this.GetToken(id));
			}

			return tokens;
		}

		protected override bool ReleaseHandle()
		{
			NativeApi.llama_free(this.handle);
			this.SetHandle(IntPtr.Zero);
			return true;
		}
	}
}