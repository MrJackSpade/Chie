using Ai.Utils.Extensions;
using Llama.Core.Exceptions;
using Llama.Core.Interfaces;
using Llama.Core.Samplers.FrequencyAndPresence;
using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Exceptions;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Data.Scheduler;
using Llama.Extensions;
using Llama.Native;
using System.Diagnostics;
using System.Text;

namespace Llama.Core
{
	public class LlamaContextWrapper : IContext
	{
		private readonly LlamaTokenCollection _buffer;

		private readonly uint _evalThreadCount;

		private readonly LlamaTokenCollection _evaluated;

		private readonly IExecutionScheduler _executionScheduler;

		private readonly LlamaContextSettings _settings;

		private readonly IList<ISimpleSampler> _simpleSamplers;

		private readonly ITokenSelector _tokenSelector;

		private uint _bufferPointer = 0;

		private uint _evalPointer = 0;

		private readonly float[,] _embeddingStack;

		public LlamaContextWrapper(IExecutionScheduler executionScheduler, SafeLlamaContextHandle handle, SafeLlamaModelHandle modelHandle, LlamaContextSettings settings, IEnumerable<ISimpleSampler> simpleSamplers, ITokenSelector tokenSelector)
		{
			if (executionScheduler is null)
			{
				throw new ArgumentNullException(nameof(executionScheduler));
			}

			if (handle is null)
			{
				throw new ArgumentNullException(nameof(handle));
			}

			if (simpleSamplers is null)
			{
				throw new ArgumentNullException(nameof(simpleSamplers));
			}

			this._embeddingStack = new float[settings.ContextSize, 8192];

			for (int x = 0; x < settings.ContextSize; x++)
			{
				for (int y = 0; y < settings.ContextSize; y++)
				{
					this._embeddingStack[x, y] = float.NaN;
				}
			}

			this._evalThreadCount = settings.EvalThreadCount;
			this._executionScheduler = executionScheduler;
			this.Handle = handle;
			this._simpleSamplers = simpleSamplers.ToList();
			this._tokenSelector = tokenSelector ?? throw new ArgumentNullException(nameof(tokenSelector));
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
			this.Size = this._settings.ContextSize;
			this._buffer = new LlamaTokenBuffer(this.Size);
			this._evaluated = new LlamaTokenBuffer(this.Size);
			this.ModelHandle = modelHandle ?? throw new ArgumentNullException();

			if (!Directory.Exists("Logits"))
			{
				Directory.CreateDirectory("Logits");
			}
		}

		protected LlamaContextWrapper()
		{
		}

		public uint AvailableBuffer => this.Size - this._bufferPointer;

		public IReadOnlyLlamaTokenCollection Buffer => this._buffer;

		public IReadOnlyLlamaTokenCollection Evaluated => this._evaluated;

		public SafeLlamaContextHandle Handle { get; private set; }

		public uint Size { get; private set; }
        public SafeLlamaModelHandle ModelHandle { get; }

        public void Clear()
		{
			this._buffer.Clear();
			this._bufferPointer = 0;
			this._evalPointer = 0;
		}

		public void Dispose() => this.Handle.Dispose();

		public uint Evaluate(ExecutionPriority priority, int count = -1)
		{
			if (count != -1)
			{
				throw new NotImplementedException();
			}

			this.Ensure();

			int start = (int)this._evalPointer;

			int end = (int)this._bufferPointer;

			LlamaTokenCollection toEvaluate = new(this._buffer.Skip(start).Take(end - start));

			Debug.WriteLine($"Evaluating: {toEvaluate.Count}");

			// evaluate tokens in batches
			// embed is typically prepared beforehand to fit within a batch, but not always
			for (uint i = 0; i < toEvaluate.Count; i += this._settings.BatchSize)
			{
				uint n_eval = (uint)(toEvaluate.Count - i);

				if (n_eval > this._settings.BatchSize)
				{
					n_eval = this._settings.BatchSize;
				}

				LlamaTokenCollection thisBlock = new(toEvaluate.Skip((int)i).Take((int)n_eval));

				int[] tokens = thisBlock.Ids.ToArray();

				try
				{
					Debug.WriteLine($"{this._evalPointer + 1}/{end}");

					this._executionScheduler.Execute(() => this.NativeEval(tokens), priority);
				}
				catch (Exception e) when (Debugger.IsAttached)
				{
					Debug.WriteLine(e);
					Debugger.Break();
				}

				for (uint c = 0; c < n_eval; c++)
				{
					int c_loc = (int)(c + this._evalPointer);

					this._evaluated[c_loc] = this._buffer[c_loc];
				}

				this._evalPointer += n_eval;
			}

			//The entirety of the token data needs to be synced for all tokens regardless
			//once the eval is complete, because otherwise metadata wont be copied across
			//The copy call above only intends on copying for the sake of the modification
			//event but an additional "full sync" call is needed.
			for (int i = 0; i < this._evalPointer; i++)
			{
				this._evaluated[i] = this._buffer[i];
			}

			if (toEvaluate.Count > 0)
			{
				//Everything after the evaluation pointer is zero'd out because its no
				//longer valid. If we don't do this, new tokens might "match" old data
				//left in the buffer and increment the pointer without triggering a new
				//eval. WE only need to do this if we've actually evaluated something
				//because a zero length eval means the data is/was a match to the previous
				//eval and the buffer is still value
				for (uint i = this._evalPointer; i < this.Evaluated.Count; i++)
				{
					this._evaluated[(int)i] = LlamaToken.Null;
				}
			}

			return toEvaluate.Count;
		}

		public LlamaToken SampleNext(LogitRuleCollection logitRules, ExecutionPriority priority) => this._executionScheduler.Execute(() => this.SampleTokenRaw(logitRules), priority);

		public LlamaToken SampleTokenRaw(LogitRuleCollection logitRules)
		{
			Span<float> logits = this.GetLogits();

			//float[] embeddings = this.GetEmbeddings();
			//
			//for (int i = 0; i < embeddings.Length; i++)
			//{
			//	this._embeddingStack[this._evalPointer, i] = embeddings[i];
			//}

			List<string> values = new(logits.Length);

			foreach (float logit in logits)
			{
				values.Add(logit.ToString());
			}

			// Apply params.logit_bias map
			logits.Add(logitRules.OfType<LogitBias>());

			LlamaTokenDataArray candidates = new(logits);

			Dictionary<LlamaToken, float> no_penalize = logits.Extract(this.NoPenalize());

			SampleContext sampleContext = new()
			{
				Candidates = candidates,
				ContextHandle = Handle,
				ContextTokens = Evaluated,
				ModelHandle = ModelHandle
			};

			foreach (LogitClamp clamp in logitRules.OfType<LogitClamp>())
			{
				clamp.SetStart(sampleContext.GetProbability(clamp.LogitId));
			}

			//TODO: Fix cheap hack
			foreach (ISimpleSampler simpleSampler in this._simpleSamplers.Where(s => s.GetType() != typeof(ComplexPresenceSampler)))
			{
				simpleSampler.SampleNext(sampleContext);
			}

			//Apply penalty
			foreach (LogitPenalty penalty in logitRules.OfType<LogitPenalty>())
			{
				sampleContext.SetPenalty(penalty.LogitId, penalty.Value);
			}

			sampleContext.Update(no_penalize);

			//TODO: Fix cheap hack
			foreach (ISimpleSampler simpleSampler in this._simpleSamplers.Where(s => s.GetType() == typeof(ComplexPresenceSampler)))
			{
				simpleSampler.SampleNext(sampleContext);
			}

			//Apply bias
			foreach (LogitBias bias in logitRules.OfType<LogitBias>())
			{
				sampleContext.SetBias(bias.LogitId, bias.Value, bias.LogitBiasType);
			}

			//Apply clamping
			foreach (LogitClamp clamp in logitRules.OfType<LogitClamp>())
			{
				float nv = sampleContext.GetProbability(clamp.LogitId);
				float cv = clamp.GetValue(nv);

				if (cv != nv)
				{
					sampleContext.SetProbability(clamp.LogitId, cv);
				}
			}

			int tokenId = this._tokenSelector.SampleNext(sampleContext);

			LlamaToken toReturn = this.GetToken(tokenId);

			return toReturn;
		}

		public void SetBufferPointer(uint startIndex)
		{
			this._bufferPointer = startIndex;

			if (this._evalPointer >= this._bufferPointer)
			{
				this._evalPointer = this._bufferPointer;
			}
			else
			{
				for (int i = 0; i < this._bufferPointer; i++)
				{
					if (this._evaluated[i] == this._buffer[i])
					{
						this._evalPointer = (uint)i;
					}
					else
					{
						return;
					}
				}
			}
		}

		public void Write(LlamaToken token)
		{
			if (this.AvailableBuffer == 0)
			{
				throw new OutOfContextException();
			}

			this._buffer[(int)this._bufferPointer] = token;

			//If the eval pointer is in sync with the buffer (up to date)
			//We need to be up to date because a single mismatch char forces
			//requires an eval for all subsequent tokens.
			if (this._evalPointer >= this._bufferPointer)
			{
				LlamaToken lastEval = this._evaluated[(int)this._bufferPointer];

				if (lastEval.Id != 0 || token.Id == 0) //sanity debug skip. Not needed
				{
					//Then check to see if the current eval token matches.
					//If it does, we dont need to eval again
					if (this._evaluated[(int)this._bufferPointer].Id == token.Id)
					{
						//Just in case theres metadata
						this._evaluated[(int)this._bufferPointer] = token;

						//Ensure we bump the eval, to keep them in sync
						this._evalPointer++;
					}
				}
			}

			this._bufferPointer++;
		}

		private void NativeEval(int[] tokens)
		{
			if (this._evalThreadCount == 0)
			{
				throw new LlamaCppRuntimeError("Evaluation thread count can not be zero");
			}

			if (NativeApi.Eval(this.Handle, tokens, tokens.Length, this._evalPointer, (int)this._evalThreadCount) != 0)
			{
				throw new LlamaCppRuntimeError("Failed to eval.");
			}
		}

		private LlamaTokenCollection NoPenalize()
		{
			LlamaTokenCollection collection = new();
			collection.Append(this.GetToken(13));//NL
			collection.Append(this.GetToken(334));// *
			collection.Append(this.GetToken(29930));//*
			collection.Append(this.GetToken(368));//ly
			collection.Append(this.GetToken(297));//in
			collection.Append(this.GetToken(591));//we
			collection.Append(this.GetToken(411));//with
			collection.Append(this.GetToken(373));//on
			collection.Append(this.GetToken(596));//your
			collection.Append(this.GetToken(445));//this
			collection.Append(this.GetToken(1048));//about
			collection.Append(this.GetToken(408));//as
			collection.Append(this.GetToken(367));//be
			collection.Append(this.GetToken(338));//is
			collection.Append(this.GetToken(470));//or
			collection.Append(this.GetToken(727));//there
			collection.Append(this.GetToken(267));//es
			collection.Append(this.GetToken(1749));//our
			collection.Append(this.GetToken(541));//but
			collection.Append(this.GetToken(769));//then
			collection.Append(this.GetToken(515));//from
			collection.Append(this.GetToken(451));//not
			collection.Append(this.GetToken(491));//by
			collection.Append(this.GetToken(577));//so
			collection.Append(this.GetToken(502));//us
			collection.Append(this.GetToken(526));//are
			collection.Append(this.GetToken(437));//do
			collection.Append(this.GetToken(565));//if
			collection.Append(this.GetToken(471));//was
			collection.Append(this.GetToken(2086));//too
			collection.Append(this.GetToken(304));//to
			collection.Append(this.GetToken(590));//my
			collection.Append(this.GetToken(902));//her
			collection.Append(this.GetToken(1075));//him
			collection.Append(this.GetToken(670));//his
			collection.Append(this.GetToken(7955));//hers
			collection.Append(this.GetToken(29892));//,
			collection.Append(this.GetToken(322));//and
			collection.Append(this.GetToken(306));//I
			collection.Append(this.GetToken(310));//of
			collection.Append(this.GetToken(29879));//s
			collection.Append(this.GetToken(29991));//!
			collection.Append(this.GetToken(278));//the
			collection.Append(this.GetToken(592));//me
			collection.Append(this.GetToken(263));//a
			collection.Append(this.GetToken(363));//for
			collection.Append(this.GetToken(372));//it
			collection.Append(this.GetToken(393));//that
			return collection;
		}
	}
}