using Llama.Core.Interfaces;
using Llama.Data.Exceptions;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;

namespace Llama.Core.Utils
{
    internal partial class KvCacheShifter : IArrayShifter<LlamaToken>
    {
        private SafeLlamaContextHandle _handle;

        private SafeLlamaModelHandle _model;

        private uint _threadCount;

        public KvCacheShifter(uint threadCount, SafeLlamaContextHandle handle, SafeLlamaModelHandle modelHandle)
        {
            _threadCount = threadCount;
            _handle = handle;
            _model = modelHandle;
        }

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        private bool TryFindBlock(BatchDecode<int> batchIds, out FoundBlock foundBlock)
        {
            var cells = NativeApi.GetKvCells(this._handle);

            foundBlock = null;

            uint firstEmpty = 0;

            while (cells[firstEmpty].pos >= 0)
            {
                firstEmpty++;

                if (firstEmpty == cells.Length)
                {
                    return false;
                }
            }

            for (uint i = firstEmpty; i < cells.Length - batchIds.Items.Count; i++)
            {
                FoundBlock thisBlock = new()
                {
                    Offset = i
                };

                bool goodBlock = true;

                uint cutoff = (uint)batchIds.Items.Count;

                uint ii = 0;

                while (ii < cutoff)
                {
                    uint offset = i + ii;

                    if (offset >= cells.Length)
                    {
                        return foundBlock != null;
                    }

                    if (cells[offset].pos > 0)
                    {
                        cutoff++;
                        thisBlock.AddReplacement(cells[offset].pos, cells[offset].value);

                        if (foundBlock != null && foundBlock.TokenReplacements.Count <= thisBlock.TokenReplacements.Count)
                        {
                            goodBlock = false;
                            break;
                        }
                    }

                    ii++;
                }

                if (goodBlock)
                {
                    thisBlock.Size = cutoff;

                    if (foundBlock == null || foundBlock.Size > thisBlock.Size)
                    {
                        foundBlock = thisBlock;

                        if (thisBlock.TokenReplacements.Count == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return foundBlock != null;
        }

        public void Decode(BatchDecode<LlamaToken> batch)
        {
            BatchDecode<int> idBatch = new();

            uint maxNatural = 0;
            uint maxReplacement = 0;

            bool reevaluate = false;

            foreach (BatchItem<LlamaToken> oldItem in batch.Items)
            {
                idBatch.AddItem(oldItem.Token.Id, oldItem.Position, oldItem.SequenceIds, oldItem.IncludeLogits);
                maxNatural = Math.Max(oldItem.Position, maxNatural);
            }

            if (idBatch.Items.Count > 1)
            {
                if (TryFindBlock(idBatch, out FoundBlock foundBlock))
                {
                    if (foundBlock.TokenReplacements.Count > 0)
                    {
                        //We will need to clear up these token slots that we found in this span
                        //so the Llama side will be able to use these slots. Queue them up for
                        //re-eval, and clear the data from the Cache. We cant move them yet
                        //so clear and batch is the only way to "move" them"
                        foreach (TokenReplacement tr in foundBlock.TokenReplacements)
                        {
                            maxReplacement = Math.Max(maxReplacement, tr.Pos);
                            idBatch.AddItem(tr.Value, tr.Pos);
                            NativeApi.RemoveCacheToken(_handle, tr.Pos);
                        }
                    }

                    if (maxReplacement > maxNatural)
                    {
                        reevaluate = true;
                    }
                }
                else
                {
                    throw new Exception("Not enough context space for batch");
                }
            }

#if DEBUG
            Log(idBatch);
#endif

            if (!reevaluate)
            {
                Eval(idBatch);
            }
            else
            {
                BatchDecode<int> b1 = idBatch.Clone(b => b.Position != maxNatural);
                BatchDecode<int> b2 = idBatch.Clone(b => b.Position == maxNatural);

                Eval(b1, b2);
            }
        }

        private void Log(BatchDecode<int> idBatch)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            string fName = Path.Combine("Logs", DateTime.Now.Ticks.ToString() + "_batch.json");

            string json = System.Text.Json.JsonSerializer.Serialize(idBatch);

            File.WriteAllText(fName, json);
        }

        private void Eval(params BatchDecode<int>[] batches)
        {
            foreach (var batch in batches)
            {
                Debug.WriteLine($"Evaluating: {batch.Items.Count}");

                int result = NativeApi.Decode(this._handle, batch);

                if (result != 0)
                {
                    throw new LlamaCppRuntimeError("Failed to eval.");
                }
            }
        }

        public void Evaluate(LlamaToken[] tokens, uint pos)
        {
            if (this._threadCount == 0)
            {
                throw new LlamaCppRuntimeError("Evaluation thread count can not be zero");
            }

            if (NativeApi.Eval(this._handle, tokens.Select(l => l.Id).ToArray(), tokens.Length, pos, (int)this._threadCount) != 0)
            {
                throw new LlamaCppRuntimeError("Failed to eval.");
            }
        }

        public int GetCacheTokenCount()
        {
            throw new NotImplementedException();
        }

        public void KeepCacheTokens(uint sequenceId)
        {
            throw new NotImplementedException();
        }

        public void RemoveCacheToken(uint index)
            => RemoveCacheTokens(index, index + 1);

        public void RemoveCacheTokens(uint startPos, uint endPos)
            => NativeApi.RemoveCacheTokens(this._handle, startPos, endPos);

        public void ShiftCacheToken(uint sequenceId, uint index, int delta)
            => NativeApi.ShiftCacheTokens(this._handle, sequenceId, index, index + 1, delta);

        public void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta)
            => NativeApi.ShiftCacheTokens(_handle, sequenceId, startPos, endPos, delta);
    }
}