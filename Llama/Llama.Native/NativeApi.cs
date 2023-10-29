using Llama.Data;
using Llama.Data.Exceptions;
using Llama.Data.Models;
using Llama.Data.Native;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Llama.Native
{
    public static class NativeApi
    {
        public static int Decode(SafeLlamaContextHandle handle, LlamaBatch batch)
        {
            var nbatch = new LlamaBatchNative
            {
                NTokens = batch.NTokens,
                Token = Marshal.UnsafeAddrOfPinnedArrayElement(batch.Tokens, 0),
                Embd = Marshal.UnsafeAddrOfPinnedArrayElement(batch.Embds, 0),
                Pos = Marshal.UnsafeAddrOfPinnedArrayElement(batch.Pos, 0),
                NSeqId = Marshal.UnsafeAddrOfPinnedArrayElement(batch.NSeqId, 0),
                SeqId = Marshal.AllocHGlobal(IntPtr.Size * batch.NTokens),
                Logits = Marshal.UnsafeAddrOfPinnedArrayElement(batch.Logits, 0)
            };

            for (int i = 0; i < batch.NTokens; i++)
            {
                IntPtr seqIdPointer = Marshal.UnsafeAddrOfPinnedArrayElement(batch.SeqId[i], 0);
                Marshal.WriteIntPtr(batch.SeqId, i * IntPtr.Size, seqIdPointer);
            }

            // Call the PInvoke method
            int result = LlamaCppApi.Decode(handle, nbatch);

            // Free the allocated memory
            Marshal.FreeHGlobal(nbatch.SeqId);

            return result;
        }

        private static void Log(string method, params object[] args)
        {
            args ??= new object[0];

            Debug.WriteLine($"{method}({string.Join(", ", args)})");
        }

        public static int Eval(SafeLlamaContextHandle handle, int[] tokens, int length, uint evalPointer, int evalThreadCount)
        {
            Log(nameof(Eval), tokens, length, evalPointer, evalThreadCount);

            return  LlamaCppApi.Eval(handle, tokens, length, (int)evalPointer, evalThreadCount);
        }

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

        public static List<int> LlamaTokenize(SafeLlamaModelHandle ctx, string text, bool add_bos, bool useLegacy = true)
        {
            if (text == "\n")
            {
                return new List<int>() { 13 };
            }

            int cnt = System.Text.Encoding.Unicode.GetByteCount(text + 1);

            int[] res = new int[cnt + (add_bos ? 1 : 0)];

            int n = LlamaCppApi.Tokenize(ctx, text, res, res.Length, add_bos);

            if (n < 0)
            {
                throw new LlamaCppRuntimeError("Error happened during tokenization. It's possibly caused by wrong encoding. Please try to specify the encoding.");
            }

            res = res.Take(n).ToArray();

            if (useLegacy && res[0] == 29871)
            {
                res = res.Skip(1).ToArray();
            }

            return res.ToList();
        }

        public static SafeLlamaContextHandle LoadContext(SafeLlamaModelHandle model, LlamaContextSettings contextSettings)
        {
            LlamaContextParams lparams = LlamaCppApi.ContextDefaultParams();

            lparams.NCtx = contextSettings.ContextSize;
            lparams.NBatch = contextSettings.BatchSize;
            lparams.Seed = contextSettings.Seed;
            lparams.F16Kv = contextSettings.MemoryMode == Llama.Data.Enums.MemoryMode.Float16;
            lparams.LogitsAll = contextSettings.Perplexity;
            lparams.Embedding = contextSettings.GenerateEmbedding;
            lparams.RopeFreqBase = contextSettings.RopeFrequencyBase;
            lparams.RopeFreqScale = contextSettings.RopeFrequencyScaling;
            lparams.MulMatQ = true;
            lparams.NThreadsBatch = contextSettings.ThreadCount;
            lparams.NThreads = contextSettings.ThreadCount;

            IntPtr ctx_ptr = LlamaCppApi.NewContextWithModel(model, lparams);

            if (ctx_ptr == IntPtr.Zero)
            {
                throw new LlamaCppRuntimeError($"Failed to load context.");
            }

            SafeLlamaContextHandle ctx = new(ctx_ptr, model, (p) => LlamaCppApi.FreeContext(p));

            if (!string.IsNullOrEmpty(contextSettings.LoraAdapter))
            {
                int err = LlamaCppApi.ApplyLoraFromFile(ctx, contextSettings.LoraAdapter, string.IsNullOrEmpty(contextSettings.LoraBase) ? null : contextSettings.LoraBase, (int)contextSettings.ThreadCount);
                if (err != 0)
                {
                    throw new LlamaCppRuntimeError("Failed to apply lora adapter.");
                }
            }

            return ctx;
        }

        public static LlamaModel LoadModel(LlamaModelSettings modelSettings)
        {
            LlamaModelParams lparams = LlamaCppApi.ModelDefaultParams();

            lparams.NGpuLayers = modelSettings.GpuLayerCount;
            lparams.UseMmap = modelSettings.UseMemoryMap;
            lparams.UseMlock = modelSettings.UseMemoryLock;

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

        public static int NVocab(SafeLlamaModelHandle handle) => LlamaCppApi.NVocab(handle);

        public static void ShiftCacheTokens(SafeLlamaContextHandle handle, uint sequenceId, uint startPos, uint endPos, int delta)
        {
            //llama_kv_cache_seq_rm(ctx, 0, params.n_keep + 1, params.n_keep + 1 + n_discard);
            //llama_kv_cache_seq_shift(ctx, 0, params.n_keep + 1 + n_discard, n_past, -n_discard);

            if(delta > 0)
            {
                throw new NotImplementedException("Right shift not implemented");
            }

            Log(nameof(LlamaCppApi.RemoveCacheTokens), sequenceId, startPos + delta, startPos);
            LlamaCppApi.RemoveCacheTokens(handle, (int)sequenceId, (int)startPos + delta, (int)startPos);

            Log(nameof(LlamaCppApi.ShiftCacheTokens), sequenceId, startPos, endPos, delta);
            LlamaCppApi.ShiftCacheTokens(handle, (int)sequenceId, (int)startPos, (int)endPos, delta);
        }

        public static string TokenToPiece(this SafeLlamaModelHandle ctx, int token)
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

        private static void SetTensors(ref LlamaModelParams param, float[] values)
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