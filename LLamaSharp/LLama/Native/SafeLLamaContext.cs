using Llama.Collections;
using Llama.Events;
using Llama.Exceptions;
using Llama.Interfaces;
using Llama.Models;
using Llama.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Llama.Native
{
	public class SafeLlamaContext
		: SafeLlamaHandleBase
	{
		private readonly int _batchSize;

		private readonly Encoding _encoding;

		private readonly int _evalThreadCount;

		private readonly LlamaTokenCollection _evaluated;

		private LlamaTokenCollection _buffer;

		public SafeLlamaContext(IntPtr handle, Encoding encoding, int evalThreadCount, int size, int batchSize)
					: base(handle)
		{
			this._encoding = encoding;
			this._evalThreadCount = evalThreadCount;
			this.Size = size;
			this._buffer = new LlamaTokenBuffer(size);
			this._buffer[0] = LlamaToken.Bos;
			this._evaluated = new LlamaTokenBuffer(size);
			this._batchSize = batchSize;
		}

		protected SafeLlamaContext()
		{
		}

		public event Action<ContextModificationEventArgs> OnContextModification;

		public int AvailableBuffer => this.Size - this.LastBufferTokenIndex;

		public IReadOnlyLlamaTokenCollection Buffer => this._buffer;

		public IReadOnlyLlamaTokenCollection Evaluated => this._evaluated;

		public int FirstUnevaluatedTokenIndex
		{
			get
			{
				for (int i = 0; i < this.Size; i++)
				{
					if (this._buffer[i] != this._evaluated[i])
					{
						return i;
					}
				}

				return -1;
			}
		}

		public int LastBufferTokenIndex
		{
			get
			{
				int toReturn = -1;

				for (int i = 0; i < this.Size; i++)
				{
					if (this._buffer[i] != LlamaToken.Null)
					{
						toReturn = i;
					}
				}

				return toReturn;
			}
		}

		public int EvaluatedTokens { get; private set; }
		public int Size { get; private set; }

		public void Clear() => this._buffer.Clear();

		public int Evaluate()
		{
			this.EnsureBuffer();

			int start = this.FirstUnevaluatedTokenIndex;

			if (this.FirstUnevaluatedTokenIndex == -1)
			{
				return 0;
			}

			int end = this.LastBufferTokenIndex + 1;

			if (end <= 0)
			{
				return 0;
			}

			int[] toEvaluate = this._buffer.Skip(start).Take(end - start).Select(t => t.Id).ToArray();

			// evaluate tokens in batches
			// embed is typically prepared beforehand to fit within a batch, but not always
			for (int i = 0; i < toEvaluate.Length; i += this._batchSize)
			{
				int buffer_offset = i + start;

				int n_eval = toEvaluate.Length - i;

				if (n_eval > this._batchSize)
				{
					n_eval = this._batchSize;
				}

				int[] tokens = toEvaluate.Skip(i).Take(n_eval).ToArray();

				this.TriggerModificationEvent(buffer_offset, n_eval);

				if (NativeApi.llama_eval(this, tokens, n_eval, buffer_offset, this._evalThreadCount) != 0)
				{
					LlamaLogger.Default.Error($"Failed to eval.");
					throw new RuntimeError("Failed to eval.");
				}

				this.EvaluatedTokens = buffer_offset + n_eval;

                for (int c = 0; c < n_eval; c++)
				{
					this._evaluated[c + buffer_offset] = this._buffer[c + buffer_offset];
				}

				this.TriggerModificationEvent(this.EvaluatedTokens);
			}

			for(int i = this.EvaluatedTokens; i < this.Evaluated.Count; i++)
			{
				this._evaluated[i] = LlamaToken.Null;
			}

            this.TriggerModificationEvent();

            return toEvaluate.Length;
		}

		private void TriggerModificationEvent(int evalIndex = -1, int evalCount = -1) => OnContextModification?.Invoke(new ContextModificationEventArgs(_evaluated, _buffer, this.FirstUnevaluatedTokenIndex, evalIndex, evalCount));

		public LlamaToken GetToken(int id, string tag) => new(id, NativeApi.llama_token_to_str(this, id), tag);

		public LlamaTokenCollection Tokenize(string value, string tag, bool addBos = false)
		{
			LlamaTokenCollection tokens = new();

			foreach (int id in Utils.Llama_tokenize(this, value, addBos, this._encoding))
			{
				tokens.Append(this.GetToken(id, tag));
			}

			return tokens;
		}

		public LlamaTokenCollection Tokenize(IEnumerable<int> value, string tag)
		{
			LlamaTokenCollection tokens = new();

			foreach (int id in value)
			{
				tokens.Append(this.GetToken(id, tag));
			}

			return tokens;
		}

		public void Write(LlamaTokenCollection evaluationQueue)
		{
			if (this.AvailableBuffer < evaluationQueue.Count)
			{
				throw new Exception("Available context buffer is less than attempted buffer size");
			}

			int start = this.LastBufferTokenIndex + 1;

			for (int i = 0; i < evaluationQueue.Count; i++)
			{
				this._buffer[i + start] = evaluationQueue[i];
			}

			this.TriggerModificationEvent();
		}

		internal void SetBuffer(IEnumerable<LlamaToken> tokens)
		{
			LlamaToken[] toSet = tokens.ToArray();

			if (toSet.Length > this.Size)
			{
				throw new ArgumentOutOfRangeException("Generated context state is larger than context size");
			}

			this._buffer = new LlamaTokenBuffer(toSet, this.Size);

			this.EnsureBuffer();

			this.TriggerModificationEvent();
		}

		protected override bool ReleaseHandle()
		{
			NativeApi.llama_free(this.handle);
			this.SetHandle(IntPtr.Zero);
			return true;
		}

		private void EnsureBuffer()
		{
			if (this.Buffer[0].Id != NativeApi.llama_token_bos())
			{
				throw new Exception("First buffer token is not BOS");
			}
		}
	}
}