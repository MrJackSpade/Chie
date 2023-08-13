using Llama.Data;
using Llama.Data.Exceptions;
using Llama.Data.Models;
using Llama.Data.Native;
using System.Runtime.InteropServices;
using System.Text;

namespace Llama.Native
{
    public static class NativeApi
    {
        public static int Eval(SafeLlamaContextHandle handle, int[] tokens, int length, int evalPointer, int evalThreadCount) => LlamaCppApi.Eval(handle, tokens, length, evalPointer, evalThreadCount);

        public static float[] GetEmbeddings(this SafeLlamaContextHandle handle)
        {
            unsafe
            {
                int n_embed = LlamaCppApi.NEmbd(handle);
                float* embeddings = LlamaCppApi.GetEmbeddings(handle);
                if (embeddings == null)
                {
                    return Array.Empty<float>();
                }

                Span<float> span = new(embeddings, n_embed);
                float[] res = new float[n_embed];
                span.CopyTo(res.AsSpan());
                return res;
            }
        }

        public static unsafe Span<float> GetLogits(SafeLlamaContextHandle ctx, int length)
        {
            float* logits = LlamaCppApi.GetLogits(ctx);
            return new Span<float>(logits, length);
        }

        public static List<int> LlamaTokenize(SafeLlamaContextHandle ctx, string text, bool add_bos, Encoding encoding)
        {
            int cnt = encoding.GetByteCount(text);
            int[] res = new int[cnt + (add_bos ? 1 : 0)];
            int n = LlamaCppApi.Tokenize(ctx, text, encoding, res, res.Length, add_bos);
            if (n < 0)
            {
                throw new LlamaCppRuntimeError("Error happened during tokenization. It's possibly caused by wrong encoding. Please try to specify the encoding.");
            }

            return res.Take(n).ToList();
        }

        public static SafeLlamaContextHandle LoadContext(SafeLlamaModelHandle model, LlamaModelSettings modelSettings, LlamaContextSettings contextSettings)
        {
            LlamaContextParams lparams = LlamaCppApi.ContextDefaultParams();

            lparams.NCtx = contextSettings.ContextSize;
            lparams.NGpuLayers = modelSettings.GpuLayerCount;
            lparams.NBatch = contextSettings.BatchSize;
            lparams.Seed = modelSettings.Seed;
            lparams.NGqa = modelSettings.UseGqa ? 8 : 1;
            lparams.F16Kv = modelSettings.MemoryMode == Llama.Data.Enums.MemoryMode.Float16;
            lparams.UseMmap = modelSettings.UseMemoryMap;
            lparams.UseMlock = modelSettings.UseMemoryLock;
            lparams.LogitsAll = modelSettings.Perplexity;
            lparams.Embedding = modelSettings.GenerateEmbedding;
            lparams.RopeFreqBase = modelSettings.RopeFrequencyBase;
            lparams.RopeFreqScale = modelSettings.RopeFrequencyScaling;
            lparams.MulMatQ = true;

            SetTensors(ref lparams, new float[16]);

            IntPtr ctx_ptr = LlamaCppApi.NewContextWithModel(model, lparams);

            if (ctx_ptr == IntPtr.Zero)
            {
                throw new LlamaCppRuntimeError($"Failed to load context {modelSettings.Model}.");
            }

            SafeLlamaContextHandle ctx = new(ctx_ptr, model, (p) => LlamaCppApi.FreeContext(p));

            if (!string.IsNullOrEmpty(modelSettings.LoraAdapter))
            {
                int err = LlamaCppApi.ApplyLoraFromFile(ctx, modelSettings.LoraAdapter, string.IsNullOrEmpty(modelSettings.LoraBase) ? null : modelSettings.LoraBase, modelSettings.ThreadCount);
                if (err != 0)
                {
                    throw new LlamaCppRuntimeError("Failed to apply lora adapter.");
                }
            }

            return ctx;
        }

        public static LlamaModel LoadModel(LlamaModelSettings modelSettings)
        {
            LlamaContextParams lparams = LlamaCppApi.ContextDefaultParams();

            lparams.NCtx = modelSettings.ContextSize;
            lparams.NGpuLayers = modelSettings.GpuLayerCount;
            lparams.NBatch = modelSettings.BatchSize;
            lparams.Seed = modelSettings.Seed;
            lparams.NGqa = modelSettings.UseGqa ? 8 : 1;
            lparams.F16Kv = modelSettings.MemoryMode == Llama.Data.Enums.MemoryMode.Float16;
            lparams.UseMmap = modelSettings.UseMemoryMap;
            lparams.UseMlock = modelSettings.UseMemoryLock;
            lparams.LogitsAll = modelSettings.Perplexity;
            lparams.Embedding = modelSettings.GenerateEmbedding;
            lparams.RopeFreqBase = modelSettings.RopeFrequencyBase;
            lparams.RopeFreqScale = modelSettings.RopeFrequencyScaling;
            lparams.MulMatQ = true;

            SetTensors(ref lparams, new float[16]);

            if (!File.Exists(modelSettings.Model))
            {
                throw new FileNotFoundException($"The model file does not exist: {modelSettings.Model}");
            }

            IntPtr model_ptr = LlamaCppApi.LoadModelFromFile(modelSettings.Model, lparams);

            if (model_ptr == IntPtr.Zero)
            {
                throw new LlamaCppRuntimeError($"Failed to load model {modelSettings.Model}.");
            }

            return new(new(model_ptr, (p) => LlamaCppApi.FreeModel(p)));
        }

        public static int NVocab(SafeLlamaContextHandle handle) => LlamaCppApi.NVocab(handle);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for method", Justification = "<Pending>")]
        public static unsafe string PtrToStringUTF8(IntPtr ptr)
        {
            return Marshal.PtrToStringUTF8(ptr);
        }

        public static string TokenToStr(SafeLlamaContextHandle handle, int id) => PtrToStringUTF8(LlamaCppApi.TokenToStr(handle, id));

        private static void SetTensors(ref LlamaContextParams param, float[] values)
        {
            // Populate your array.
            for (int i = 0; i < 16; i++)
            {
                values[i] = (float)i;
            }

            // Allocate unmanaged memory for the array.
            IntPtr tensorSplitPtr = Marshal.AllocHGlobal(16 * sizeof(float));

            // Copy the managed array to unmanaged memory.
            Marshal.Copy(values, 0, tensorSplitPtr, 16);

            // Now you can set the pointer in your structure.
            param.TensorSplit = tensorSplitPtr;
        }
    }
}