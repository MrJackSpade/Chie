using Llama.Core.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Models;
using System.Diagnostics;

namespace Llama.Core.Utils
{
    public partial class PointerArraySynchronizer<T>
    {
        protected IArrayShifter<T> _arrayShifter;

        private readonly T _defaultToken;

        public PointerArraySynchronizer(IArrayShifter<T> shifter, T defaultT)
        {
            _arrayShifter = shifter;
            _defaultToken = defaultT;
        }

        public void Log(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            Guid g = Guid.NewGuid();

            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            string e_dump = $"Logs\\{g}_kvCache";
            string b_dump = $"Logs\\{g}_buffer_{buffer.Pointer}";

            File.WriteAllLines(e_dump, GetLines(kvCache));
            File.WriteAllLines(b_dump, GetLines(buffer));
        }

        public void Sync(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
#if DEBUG
            Log(kvCache, buffer);
#endif

            Debug.WriteLine("Transforming... ");
            Debug.WriteLine($"\tBuffer Pointer: {buffer.Pointer}");

            TranformCache(kvCache, buffer);

            Debug.WriteLine("Filling... ");
            Debug.WriteLine($"\tBuffer Pointer: {buffer.Pointer}");
            FillCache(kvCache, buffer);

            Debug.WriteLine("Done Evaluating. ");
            Debug.WriteLine($"\tBuffer Pointer: {buffer.Pointer}");
        }

        public void TranformCache(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            kvCache.ClearTransformations();

            //Pre Buffer
            PinUnchangedTokens(kvCache, buffer);

            FindTokenReplacements(kvCache, buffer);

            //Post Buffer
            DefragPostBuffer(kvCache, buffer);

            //Execute
            List<KvCacheTransformation<T>> requiredMoves = kvCache.GetMoves().ToList();

            //Migrate anything thats relocating out
            MoveTokensToTempSpace(kvCache, requiredMoves);

            //Clear anything thats left behind and not pinned
            ClearUnpinnedTokens(kvCache, buffer);

            //Pull tokens back in from temp space
            RetrieveFromTempSpace(kvCache, requiredMoves);
        }

        private static void PinUnchangedTokens(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            //First stage, copy over exact tokens
            for (uint i = 0; i < buffer.Pointer; i++)
            {
                T item = kvCache[i];

                if (Equals(item, buffer[i]))
                {
                    kvCache.Pin(i);
                }
            }
        }

        /// <summary>
        /// Remove any tokens that aren't set to be moved elsewhere, or pinned.
        /// If its not set to be used somewhere else and its not pinned, then
        /// it's not used anywhere. Leaving it will cause double tokens for that
        /// slot
        /// </summary>
        /// <param name="kvCache"></param>
        /// <param name="buffer"></param>
        private void ClearUnpinnedTokens(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            for (uint i = 0; i < buffer.Pointer; i++)
            {
                if (!kvCache.IsMoved(i) && !kvCache.IsDefault(i))
                {
                    _arrayShifter.RemoveCacheToken(i);
                    kvCache[i] = _defaultToken;
                }
            }
        }

        private void Decode(KvCacheState<T> kvCache, BatchDecode<T> llamaBatch)
        {
            if (llamaBatch.Items.Count > 0)
            {
                _arrayShifter.Decode(llamaBatch);

                foreach (BatchItem<T> item in llamaBatch.Items)
                {
                    kvCache[item.Position] = item.Token;
                }

                llamaBatch.Clear();
            }
        }

        private void DefragPostBuffer(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            uint ii = buffer.Length - 1;

            //Next pass. Find unused tokens and try and see if we have free space for them
            //To save cache shit later
            for (uint i = buffer.Length - 1; i <= 0; i--)
            {
                if (ii == buffer.Pointer)
                {
                    //No more space!
                    break;
                }

                //Already moved, no worries
                if (kvCache.IsMoved(i))
                {
                    continue;
                }

                //something new already assigned
                if (kvCache.IsSet(ii))
                {
                    continue;
                }

                if (kvCache.IsDefault(i))
                {
                    continue;
                }

                //Mark it to move to this space
                kvCache.Move(i, ii);

                ii--;
            }
        }

        private void FillCache(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            BatchDecode<T> llamaBatch = new();

            for (uint i = 0; i < buffer.Pointer; i++)
            {
                if (IsDefault(buffer[i]))
                {
                    throw new Exception("Default token found in buffer");
                }

                if (!Equals(kvCache[i], buffer[i]))
                {
                    llamaBatch.AddItem(buffer[i], i);
                }
            }

            Decode(kvCache, llamaBatch);
        }

        private void FindTokenReplacements(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            WrapAroundCounter e = new(kvCache.Length);

            //next stage, fill in gaps missing from the buffer
            for (uint i = 0; i < buffer.Pointer - 1; i++)
            {
                //If this slot is taken, we've already found a match
                if (kvCache.IsSet(i))
                {
                    continue;
                }

                uint end_e = e;

                T targetToken = buffer[i];

                if (IsDefault(targetToken))
                {
                    continue;
                }

                do
                {
                    //skip this one if we've already reassigned it
                    if (kvCache.IsMoved(e))
                    {
                        e.Increment();
                        continue;
                    }

                    T checkValue = kvCache[e];

                    if (Equals(targetToken, checkValue)) //and it matches what we need
                    {
                        kvCache.Move(e, i);

                        e.Increment();
                        break;
                    }

                    e.Increment();
                } while (e != end_e);
            }
        }

        private IEnumerable<string> GetLines(PointerArray<T> source)
        {
            foreach (T s in source)
            {
                if (s is LlamaToken token)
                {
                    yield return $"{token.Id}|{token.Value}";
                }
                else
                {
                    yield return $"0|";
                }
            }
        }

        private IEnumerable<string> GetLines(KvCacheState<T> source)
        {
            for (uint i = 0; i < source.Length; i++)
            {
                T s = source[i];

                if (s is LlamaToken token)
                {
                    yield return $"{token.Id}|{token.Value}";
                }
                else
                {
                    yield return $"0|";
                }
            }
        }

        private bool IsDefault(T toTest)
        {
            return Equals(_defaultToken, toTest);
        }

        private void MoveTokensToTempSpace(KvCacheState<T> kvCache, List<KvCacheTransformation<T>> requiredMoves)
        {
            //We're going to use a position outside the array as temporary.
            //If we don't we have to calculate a proper order for swapping
            //and thats a huge deal.Right now this isn't validated, but that
            //may change causing this to break in the future
            foreach (KvCacheTransformation<T> si in requiredMoves)
            {
                //Move into temp position outside the bounds
                uint tempPos = si.NewIndex + kvCache.Length;
                int tempDelta = (int)(tempPos - si.OriginalIndex);
                _arrayShifter.ShiftCacheToken(0, si.OriginalIndex, tempDelta);
                kvCache[si.OriginalIndex] = _defaultToken;
            }
        }

        private void RetrieveFromTempSpace(KvCacheState<T> kvCache, List<KvCacheTransformation<T>> requiredMoves)
        {
            //Move everything back into the array
            foreach (KvCacheTransformation<T> si in requiredMoves)
            {
                //Move into temp position outside the bounds
                uint tempPos = si.NewIndex + kvCache.Length;
                //move back size is fixed because the proper offset is calculated on the last step
                _arrayShifter.ShiftCacheToken(0, tempPos, 0 - (int)kvCache.Length);
                //Adjust the actual buffer for reals
                kvCache[si.NewIndex] = si.Item;
            }
        }
    }
}