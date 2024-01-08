using Llama.Data;
using Llama.Data.Enums;
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
        static NativeApi()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static int Decode(SafeLlamaContextHandle handle, BatchDecode<int> batch, uint maxBatchSize)
        {
            //for logging
            uint toDecode = (uint)batch.Items.Count;
            uint decoded = 0;

            //if we have to reprocess a token thats AFTER our current token, the logits will be all fucked up
            //when were done. We need to make SURE that the last token we process is what we originall expected
            uint originalHighestPosition = batch.Items.Max(b => b.Position);
            uint currentHighestPosition = originalHighestPosition;

            //grab the items
            List<BatchItem<int>> batchItems = batch.Items.ToList();

            if (batch.Embeddings != null)
            {
                throw new NotImplementedException();
            }

            if (batch.Logits != null)
            {
                throw new NotImplementedException(batch.Logits.ToString());
            }

            do
            {
                BatchDecode<int> thisBatch = new();

                //Always process in order of position
                batchItems = batchItems.OrderBy(b => b.Position).ToList();

                //Our estimated batch size
                uint thisBatchSize = Math.Min(maxBatchSize, (uint)batchItems.Count);

                //Add the tokens to the batch
                for (int i = 0; i < thisBatchSize; i++)
                {
                    thisBatch.AddItem(batchItems[0]);
                    batchItems.RemoveAt(0);
                }

                //Now we try and find room
                if (!TryFindBlock(handle, thisBatchSize, maxBatchSize, out FoundBlock fb))
                {
                    //if no room, we fail
                    return 1;
                }

                //Did we need to displace anything to find this room?
                foreach (TokenReplacement tr in fb.TokenReplacements)
                {
                    //We're going to need to redecode it
                    toDecode++;

                    //clear the slot so the cpp dll can use the space
                    RemoveCacheToken(handle, tr.Pos);

                    //is it after our current highest position token? Track that
                    currentHighestPosition = Math.Max(currentHighestPosition, tr.Pos);

                    //Do we have room in this batch?
                    if (thisBatch.Count < maxBatchSize)
                    {
                        //If so, eval it now. We have the space
                        thisBatch.AddItem(tr.Value, tr.Pos);
                    }
                    else
                    {
                        //If not, save it for the next pass
                        batchItems.Add(new BatchItem<int>(tr.Value, tr.Pos));
                    }
                }

                //If we've already found the need to move something that will
                //fuck up our decode order
                if (currentHighestPosition > originalHighestPosition)
                {
                    //And we have more than one item
                    if (thisBatch.Count > 0)
                    {
                        //And one of those items is the expected last token
                        if (thisBatch.TryRemove(originalHighestPosition, out BatchItem<int> found))
                        {
                            //remove it, because we need to process it on its own later
                            batchItems.Add(found);
                        }
                    }
                }

                thisBatchSize = (uint)thisBatch.Count;

                if (thisBatchSize > 1)
                {
                    //Log progress for sanity
                    Debug.WriteLine($"[{decoded + thisBatchSize}/{toDecode}]");
                }

                //Actually process this batch
                int result = ProcessBatch(handle, thisBatch);

                if (result != 0)
                {
                    return result;
                }

                decoded += thisBatchSize;
            } while (batchItems.Count > 0);

            return 0;
        }

        public static int Eval(SafeLlamaContextHandle handle, int[] tokens, int length, uint evalPointer, int evalThreadCount)
        {
            Log(nameof(Eval), tokens, length, evalPointer, evalThreadCount);

            return LlamaCppApi.Eval(handle, tokens, length, (int)evalPointer, evalThreadCount);
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

        public static LlamaToken[] GetEvaluated(SafeLlamaContextHandle context, SafeLlamaModelHandle model)
        {
            LlamaKvCell[] cells = GetKvCells(context);

            LlamaToken[] evaluated = new LlamaToken[cells.Length];

            Dictionary<int, List<CellDefinition>> cellDict = new();

            {
                int i = 0;
                foreach (LlamaKvCell cell in cells)
                {
                    if (cell.pos == -1)
                    {
                        continue;
                    }

                    if (!cellDict.TryGetValue(cell.pos, out List<CellDefinition>? cellColl))
                    {
                        cellColl = new List<CellDefinition>();
                        cellDict[cell.pos] = cellColl;
                    }

                    cellColl.Add(new CellDefinition()
                    {
                        Index = i,
                        Cell = cell,
                    });

                    i++;
                }
            }

            foreach (int key in cellDict.Keys)
            {
                if (cellDict[key].Count < 2)
                {
                    cellDict.Remove(key);
                }
            }

            if (cellDict.Count > 0)
            {
                Debugger.Break();
            }

            foreach (LlamaKvCell cell in cells)
            {
                LlamaToken token;

                if (cell.pos < 0)
                {
                    continue;
                }

                if (cell.value == -1)
                {
                    token = new LlamaToken(-1, null);
                }
                else
                {
                    token = new LlamaToken(cell.value, TokenToPiece(model, cell.value));
                }

                if (evaluated[cell.pos] != null)
                {
                    //throw new InvalidOperationException("Can not double assign token");
                }
                else
                {
                    evaluated[cell.pos] = token;
                }
            }

            for (int i = 0; i < evaluated.Length; i++)
            {
                if (evaluated[i] == null)
                {
                    evaluated[i] = new LlamaToken(-1, null);
                }
            }

            return evaluated;
        }

        public static LlamaKvCache GetKvCache(SafeLlamaContextHandle context)
        {
            IntPtr kvCachePtr = LlamaCppApi.GetKvCache(context);
            return Marshal.PtrToStructure<LlamaKvCache>(kvCachePtr);
        }

        public static LlamaKvCell[] GetKvCells(SafeLlamaContextHandle context)
        {
            LlamaKvCache cache = GetKvCache(context);

            uint count = cache.size;

            LlamaKvCell[] cells = new LlamaKvCell[count];

            int cellSize = Marshal.SizeOf<LlamaKvCell>();

            for (int i = 0; i < count; i++)
            {
                cells[i] = Marshal.PtrToStructure<LlamaKvCell>(IntPtr.Add(cache.cellsPointer, i * cellSize));
            }

            return cells;
        }

        public static unsafe Span<float> GetLogits(SafeLlamaContextHandle ctx, int length)
        {
            float* logits = LlamaCppApi.GetLogits(ctx);
            return new Span<float>(logits, length);
        }

        public static List<int> LlamaTokenize(SafeLlamaModelHandle ctx, string text, bool add_bos, bool useLegacy = true)
        {
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
            lparams.TypeV = GgmlType.GGML_TYPE_F16;
            lparams.TypeK = contextSettings.TypeK;
            lparams.LogitsAll = contextSettings.Perplexity;
            lparams.Embedding = contextSettings.GenerateEmbedding;
            lparams.RopeFreqBase = contextSettings.RopeFrequencyBase;
            lparams.RopeFreqScale = contextSettings.RopeFrequencyScaling;
            lparams.MulMatQ = true;
            lparams.NThreadsBatch = contextSettings.ThreadCount;
            lparams.NThreads = contextSettings.ThreadCount;
            lparams.RopeScalingType = (sbyte)contextSettings.RopeScalingType;
            lparams.YarnBetaSlow = contextSettings.YarnBetaSlow;
            lparams.YarnBetaFast = contextSettings.YarnBetaFast;
            lparams.YarnAttnFactor = contextSettings.YarnAttnFactor;
            lparams.YarnExtFactor = contextSettings.YarnExtFactor;
            lparams.OffloadKQV = contextSettings.OffloadKQV;

            if (contextSettings.YarnOrigCtx == 0)
            {
                lparams.YarnOrigCtx = contextSettings.ContextSize;
            }
            else
            {
                lparams.YarnOrigCtx = contextSettings.YarnOrigCtx;
            }

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

        public static int NVocab(SafeLlamaModelHandle handle)
        {
            return LlamaCppApi.NVocab(handle);
        }

        public static void RemoveCacheToken(SafeLlamaContextHandle handle, uint pos)
        {
            LlamaCppApi.RemoveCacheTokens(handle, -1, (int)pos, (int)(pos + 1));
        }

        public static void RemoveCacheTokens(SafeLlamaContextHandle handle, uint startPos, uint endPos)
        {
            LlamaCppApi.RemoveCacheTokens(handle, -1, (int)startPos, (int)endPos);
        }

        public static void ShiftCacheTokens(SafeLlamaContextHandle handle, uint sequenceId, uint startPos, uint endPos, int delta)
        {
            LlamaCppApi.ShiftCacheTokens(handle, (int)sequenceId, (int)startPos, (int)endPos, delta);
        }

        public static void Test()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            LlamaCppApi.EmptyCall();
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

        private static void Log(string method, params object[] args)
        {
            args ??= Array.Empty<object>();

            Debug.WriteLine($"{method}({string.Join(", ", args)})");
        }

        private static int ProcessBatch(SafeLlamaContextHandle handle, BatchDecode<int> thisBatch)
        {
            int[] tokens = new int[thisBatch.Items.Count];
            int[] pos = new int[thisBatch.Items.Count];
            int[] nseq = new int[thisBatch.Items.Count];

            for (int i = 0; i < thisBatch.Items.Count; i++)
            {
                tokens[i] = thisBatch.Items[i].Token;
                pos[i] = (int)thisBatch.Items[i].Position;
                nseq[i] = thisBatch.Items[i].SequenceIds.Length;
            }

            LlamaBatchNative nbatch = new()
            {
                NTokens = thisBatch.Items.Count,
                Token = Marshal.UnsafeAddrOfPinnedArrayElement(tokens, 0),
                Pos = Marshal.UnsafeAddrOfPinnedArrayElement(pos, 0),
                NSeqId = Marshal.UnsafeAddrOfPinnedArrayElement(nseq, 0),
                SeqId = Marshal.AllocHGlobal(IntPtr.Size * thisBatch.Items.Count)
            };

            if (thisBatch.Logits != null)
            {
                nbatch.Logits = Marshal.UnsafeAddrOfPinnedArrayElement(thisBatch.Logits, 0);
            }

            if (thisBatch.Embeddings != null)
            {
                nbatch.Embd = Marshal.UnsafeAddrOfPinnedArrayElement(thisBatch.Embeddings, 0);
            }

            // Allocate and set the unmanaged memory for the sequence IDs
            for (int i = 0; i < thisBatch.Items.Count; i++)
            {
                int[] currentSeqIds = thisBatch.Items[i].SequenceIds;
                IntPtr unmanagedArray = Marshal.AllocHGlobal(sizeof(int) * currentSeqIds.Length);

                // Copy the managed array to the unmanaged memory
                Marshal.Copy(currentSeqIds, 0, unmanagedArray, currentSeqIds.Length);

                // Set the pointer in the SeqId array
                Marshal.WriteIntPtr(nbatch.SeqId, i * IntPtr.Size, unmanagedArray);
            }

            // Call the PInvoke method
            int result = LlamaCppApi.Decode(handle, nbatch);

            // Free the allocated memory
            Marshal.FreeHGlobal(nbatch.SeqId);

            return result;
        }

        private static void SetTensors(ref LlamaModelParams param, float[] values)
        {
            // Populate your array.
            for (int i = 0; i < 16; i++)
            {
                values[i] = i;
            }

            // Allocate unmanaged memory for the array.
            IntPtr tensorSplitPtr = Marshal.AllocHGlobal(16 * sizeof(float));

            // Copy the managed array to unmanaged memory.
            Marshal.Copy(values, 0, tensorSplitPtr, 16);

            // Now you can set the pointer in your structure.
            param.TensorSplit = tensorSplitPtr;
        }

        private static bool TryFindBlock(SafeLlamaContextHandle handle, uint size, uint maxSize, out FoundBlock? foundBlock)
        {
            foundBlock = null;

            LlamaKvCell[] cells = GetKvCells(handle);

            List<uint> checkCells = new(cells.Length);

            uint emptyCells = 0;

            uint endStart = (uint)(cells.Length - size);

            for (uint i = 0; i < cells.Length; i++)
            {
                if (cells[i].pos < 0)
                {
                    if (i <= endStart)
                    {
                        checkCells.Add(i);
                    }

                    emptyCells++;
                }
            }

            if (emptyCells < size)
            {
                foundBlock = null;
                return false;
            }

            foreach (uint i in checkCells)
            {
                FoundBlock thisBlock = new()
                {
                    Offset = i,
                    RequestedSize = size
                };

                uint requiredSize = size;

                uint thisSize = 0;

                bool overrun = false;

                while (thisSize < requiredSize && thisSize < maxSize)
                {
                    uint offset = i + thisSize;

                    if (offset >= cells.Length)
                    {
                        overrun = true;
                        break;
                    }

                    if (cells[offset].pos > 0)
                    {
                        requiredSize++;

                        thisBlock.AddReplacement(cells[offset].pos, cells[offset].value);

                        if (foundBlock != null && foundBlock.ActualSize <= thisBlock.ActualSize)
                        {
                            break;
                        }
                    }

                    thisSize++;
                }

                if (overrun)
                {
                    break;
                }

                if (foundBlock == null || foundBlock.ActualSize > thisBlock.ActualSize)
                {
                    foundBlock = thisBlock;

                    if (thisBlock.TokenReplacements.Count == 0)
                    {
                        return true;
                    }
                }
            }

            return foundBlock != null;
        }

        private class CellDefinition
        {
            public LlamaKvCell Cell { get; set; }

            public int Index { get; set; }
        }
    }
}