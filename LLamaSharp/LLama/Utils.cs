using LLama.Exceptions;
using LLama.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using llama_token = System.Int32;

namespace LLama
{
	internal static class Utils
	{
		public static SafeLLamaContextHandle llama_init_from_gpt_params(ref LLamaParams @params)
		{
			LLamaContextParams lparams = NativeApi.llama_context_default_params();

			lparams.n_ctx = @params.ContextSize;
			lparams.n_gpu_layers = @params.GpuLayerCount;
			lparams.seed = @params.Seed;
			lparams.f16_kv = @params.MemoryFloat16;
			lparams.use_mmap = @params.UseMemoryMap;
			lparams.use_mlock = @params.UseMemoryLock;
			lparams.logits_all = @params.Perplexity;
			lparams.embedding = @params.GenerateEmbedding;

			if (!File.Exists(@params.Model))
			{
				throw new FileNotFoundException($"The model file does not exist: {@params.Model}");
			}

			IntPtr ctx_ptr = NativeApi.llama_init_from_file(@params.Model, lparams);

			if (ctx_ptr == IntPtr.Zero)
			{
				throw new RuntimeError($"Failed to load model {@params.Model}.");
			}

			SafeLLamaContextHandle ctx = new(ctx_ptr);

			if (!string.IsNullOrEmpty(@params.LoraAdapter))
			{
				int err = NativeApi.llama_apply_lora_from_file(ctx, @params.LoraAdapter,
					string.IsNullOrEmpty(@params.LoraBase) ? null : @params.LoraBase, @params.ThreadCount);
				if (err != 0)
				{
					throw new RuntimeError("Failed to apply lora adapter.");
				}
			}

			return ctx;
		}

		public static List<llama_token> llama_tokenize(SafeLLamaContextHandle ctx, string text, bool add_bos, string encodingName)
		{
			Encoding encoding = Encoding.GetEncoding(encodingName);
			int cnt = encoding.GetByteCount(text);
			llama_token[] res = new llama_token[cnt + (add_bos ? 1 : 0)];
			int n = NativeApi.llama_tokenize(ctx, text, encoding, res, res.Length, add_bos);
			if (n < 0)
			{
				throw new RuntimeError("Error happened during tokenization. It's possibly caused by wrong encoding. Please try to " +
					"specify the encoding.");
			}

			return res.Take(n).ToList();
		}

		public static unsafe Span<float> llama_get_logits(SafeLLamaContextHandle ctx, int length)
		{
			float* logits = NativeApi.llama_get_logits(ctx);
			return new Span<float>(logits, length);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for method", Justification = "<Pending>")]
		public static unsafe string PtrToStringUTF8(IntPtr ptr)
		{
#if NET6_0_OR_GREATER
			return Marshal.PtrToStringUTF8(ptr);
#else
			byte* tp = (byte*)ptr.ToPointer();
			List<byte> bytes = new();
			while (true)
			{
				byte c = *tp++;
				if (c == '\0')
				{
					break;
				}
				else
				{
					bytes.Add(c);
				}
			}

			return Encoding.UTF8.GetString(bytes.ToArray());
#endif
		}
	}
}