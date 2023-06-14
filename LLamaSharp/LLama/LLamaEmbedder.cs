using Llama.Exceptions;
using Llama.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace Llama
{
	public class LlamaEmbedder : IDisposable
	{
		private readonly SafeLlamaContext _ctx;

		private readonly int _threads;

		public LlamaEmbedder(LlamaModelSettings @params, Encoding encoding, int threads)
		{
			@params.GenerateEmbedding = true;
			this._threads = threads;

			if (this._threads == -1)
			{
				this._threads = Math.Max(Environment.ProcessorCount / 2, 1);
			}

			this._ctx = Utils.Llama_init_from_gpt_params(ref @params, encoding);
		}

		/// <summary>
		/// Warning: must ensure the original model has params.embedding = true;
		/// </summary>
		/// <param name="ctx"></param>
		internal LlamaEmbedder(SafeLlamaContext ctx)
		{
			this._ctx = ctx;
		}

		public void Dispose() => this._ctx.Dispose();

		public unsafe float[] GetEmbeddings(string text, Encoding encoding, bool add_bos = true)
		{
			int n_past = 0;
			if (add_bos)
			{
				text = text.Insert(0, " ");
			}

			List<int> embed_inp = Utils.Llama_tokenize(this._ctx, text, add_bos, encoding);

			// TODO(Rinne): deal with log of prompt

			if (embed_inp.Count > 0)
			{
				int[] embed_inp_array = embed_inp.ToArray();
				if (NativeApi.llama_eval(this._ctx, embed_inp_array, embed_inp_array.Length, n_past, this._threads) != 0)
				{
					throw new RuntimeError("Failed to eval.");
				}
			}

			int n_embed = NativeApi.llama_n_embd(this._ctx);
			float* embeddings = NativeApi.llama_get_embeddings(this._ctx);
			if (embeddings == null)
			{
				return new float[0];
			}

			Span<float> span = new(embeddings, n_embed);
			float[] res = new float[n_embed];
			span.CopyTo(res.AsSpan());
			return res;
		}
	}
}