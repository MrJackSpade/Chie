using Llama.Data;
using Llama.Data.Exceptions;
using Llama.Data.Models;
using Llama.Data.Native;
using System.Dynamic;
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

        public static List<int> LlamaTokenize(SafeLlamaContextHandle ctx, string text, bool add_bos, Encoding encoding, bool useLegacy = true)
        {
            if(text == "\n")
            {
                return new List<int>() { 13 };
            }

            int cnt = encoding.GetByteCount(text + 1);

            int[] res = new int[cnt + (add_bos ? 1 : 0)];
            
            int n = LlamaCppApi.Tokenize(ctx, text, encoding, res, res.Length, add_bos);

            if (n < 0)
            {
                throw new LlamaCppRuntimeError("Error happened during tokenization. It's possibly caused by wrong encoding. Please try to specify the encoding.");
            }

            res = res.Take(n).ToArray();

            if(useLegacy && res[0] == 29871)
            {
                res = res.Skip(1).ToArray();
            }

            return res.ToList();
        }

        public static SafeLlamaContextHandle LoadContext(SafeLlamaModelHandle model, LlamaModelSettings modelSettings, LlamaContextSettings contextSettings)
        {
            LlamaContextParams lparams = LlamaCppApi.ContextDefaultParams();

            lparams.NCtx = contextSettings.ContextSize;
            lparams.NGpuLayers = modelSettings.GpuLayerCount;
            lparams.NBatch = contextSettings.BatchSize;
            lparams.Seed = modelSettings.Seed;
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

		public static string TokenToPiece(this SafeLlamaContextHandle ctx, int token)
		{
			// Assuming a buffer size of 256, adjust as needed.
			char[] buffer = new char[256];

			int result = LlamaCppApi.TokenToPiece(ctx, token, buffer, buffer.Length);

			// Assuming a successful result is indicated by a non-negative value.
			// Adjust the condition based on the actual behavior of the C++ function.
			if (result < 0)
			{
				throw new InvalidOperationException($"Failed to convert token to piece. Error code: {result}");
			}

			string toReturn = new(buffer, 0, result);

			byte[] dataAsWindows1252 = Encoding.GetEncoding("Windows-1252").GetBytes(toReturn);

			string correctlyInterpretedString = Encoding.UTF8.GetString(dataAsWindows1252);

			return correctlyInterpretedString;
		}

		public static LlamaModel LoadModel(LlamaModelSettings modelSettings)
        {
            LlamaContextParams lparams = LlamaCppApi.ContextDefaultParams();

            lparams.NCtx = modelSettings.ContextSize;
            lparams.NGpuLayers = modelSettings.GpuLayerCount;
            lparams.NBatch = modelSettings.BatchSize;
            lparams.Seed = modelSettings.Seed;
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