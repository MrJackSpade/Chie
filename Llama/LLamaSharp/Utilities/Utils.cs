﻿using Llama.Context;
using Llama.Exceptions;
using Llama.Model;
using Llama.Native;
using Llama.Native.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Llama_token = System.Int32;

namespace Llama.Utilities
{
    public static class Utils
    {
        public static SafeLlamaModelHandle InitModelFromParams(LlamaModelSettings modelSettings, LlamaContextSettings contextSettings)
        {
            LlamaContextParams lparams = NativeApi.ContextDefaultParams();

            lparams.n_ctx = contextSettings.ContextSize;
            lparams.n_gpu_layers = modelSettings.GpuLayerCount;
            lparams.n_batch = contextSettings.BatchSize;
            lparams.seed = modelSettings.Seed;
            lparams.f16_kv = modelSettings.MemoryFloat16;
            lparams.use_mmap = modelSettings.UseMemoryMap;
            lparams.use_mlock = modelSettings.UseMemoryLock;
            lparams.logits_all = modelSettings.Perplexity;
            lparams.embedding = modelSettings.GenerateEmbedding;

            if (!File.Exists(modelSettings.Model))
            {
                throw new FileNotFoundException($"The model file does not exist: {modelSettings.Model}");
            }

            IntPtr model_ptr = NativeApi.LoadModelFromFile(modelSettings.Model, lparams);

            if (model_ptr == IntPtr.Zero)
            {
                throw new RuntimeError($"Failed to load model {modelSettings.Model}.");
            }

            return new(model_ptr);
        }

        public static SafeLlamaContextHandle InitContextFromParams(SafeLlamaModelHandle model, LlamaModelSettings modelSettings, LlamaContextSettings contextSettings)
        {
            LlamaContextParams lparams = NativeApi.ContextDefaultParams();

            lparams.n_ctx = contextSettings.ContextSize;
            lparams.n_gpu_layers = modelSettings.GpuLayerCount;
            lparams.n_batch = contextSettings.BatchSize;
            lparams.seed = modelSettings.Seed;
            lparams.f16_kv = modelSettings.MemoryFloat16;
            lparams.use_mmap = modelSettings.UseMemoryMap;
            lparams.use_mlock = modelSettings.UseMemoryLock;
            lparams.logits_all = modelSettings.Perplexity;
            lparams.embedding = modelSettings.GenerateEmbedding;

            IntPtr ctx_ptr = NativeApi.NewContextWithModel(model, lparams);

            if (ctx_ptr == IntPtr.Zero)
            {
                throw new RuntimeError($"Failed to load context {modelSettings.Model}.");
            }

            SafeLlamaContextHandle ctx = new(ctx_ptr, model);

            if (!string.IsNullOrEmpty(modelSettings.LoraAdapter))
            {
                int err = NativeApi.ApplyLoraFromFile(ctx, modelSettings.LoraAdapter, string.IsNullOrEmpty(modelSettings.LoraBase) ? null : modelSettings.LoraBase, modelSettings.ThreadCount);
                if (err != 0)
                {
                    throw new RuntimeError("Failed to apply lora adapter.");
                }
            }

            return ctx;
        }

        public static SafeLlamaContextHandle InitContextFromParams(LlamaModelSettings modelSettings, LlamaContextSettings contextSettings)
        {
            SafeLlamaModelHandle model = InitModelFromParams(modelSettings, contextSettings);
            return InitContextFromParams(model, modelSettings, contextSettings);
        }

        public static List<Llama_token> LlamaTokenize(SafeLlamaContextHandle ctx, string text, bool add_bos, Encoding encoding)
        {
            int cnt = encoding.GetByteCount(text);
            Llama_token[] res = new Llama_token[cnt + (add_bos ? 1 : 0)];
            int n = NativeApi.Tokenize(ctx, text, encoding, res, res.Length, add_bos);
            if (n < 0)
            {
                throw new RuntimeError("Error happened during tokenization. It's possibly caused by wrong encoding. Please try to specify the encoding.");
            }

            return res.Take(n).ToList();
        }

        public static unsafe Span<float> GetLogits(SafeLlamaContextHandle ctx, int length)
        {
            float* logits = NativeApi.GetLogits(ctx);
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