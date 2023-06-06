using LLama.Exceptions;
using LLama.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLama
{
	public class LLamaEmbedder : IDisposable
	{
		private readonly SafeLLamaContextHandle _ctx;

		public LLamaEmbedder(LlamaModelSettings @params)
		{
			@params.GenerateEmbedding = true;
			this._ctx = Utils.llama_init_from_gpt_params(ref @params);
		}

		/// <summary>
		/// Warning: must ensure the original model has params.embedding = true;
		/// </summary>
		/// <param name="ctx"></param>
		internal LLamaEmbedder(SafeLLamaContextHandle ctx)
		{
			this._ctx = ctx;
		}

		public void Dispose() => this._ctx.Dispose();

		public unsafe float[] GetEmbeddings(string text, Encoding encoding, int n_thread = -1, bool add_bos = true)
		{
			if (n_thread == -1)
			{
				n_thread = Math.Max(Environment.ProcessorCount / 2, 1);
			}

			int n_past = 0;
			if (add_bos)
			{
				text = text.Insert(0, " ");
			}

			List<int> embed_inp = Utils.llama_tokenize(this._ctx, text, add_bos, encoding);

			// TODO(Rinne): deal with log of prompt

			if (embed_inp.Count > 0)
			{
				int[] embed_inp_array = embed_inp.ToArray();
				if (NativeApi.llama_eval(this._ctx, embed_inp_array, embed_inp_array.Length, n_past, n_thread) != 0)
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