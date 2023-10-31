using Llama.Core.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Models;
using System.Data;
using System.Diagnostics;

namespace Llama.Core.Utils
{
    public class PointerArraySynchronizer<T>
    {
        protected IArrayShifter<T> _arrayShifter;

        private readonly uint _batchSize;

        private readonly T _defaultT;

        public PointerArraySynchronizer(IArrayShifter<T> shifter, uint batchSize, T defaultT)
        {
            _arrayShifter = shifter;
            _batchSize = batchSize;
            _defaultT = defaultT;
        }

        public void Sync(PointerArray<T> evaluated, PointerArray<T> buffer)
        {
            Debug.WriteLine("Shifting... ");
            Debug.WriteLine($"\tEvaluated Pointer: {evaluated.Pointer}");
            Debug.WriteLine($"\tBuffer Pointer: {buffer.Pointer}");

            ShiftPhase(evaluated, buffer);

            Debug.WriteLine("Evaluating... ");
            Debug.WriteLine($"\tEvaluated Pointer: {evaluated.Pointer}");
            Debug.WriteLine($"\tBuffer Pointer: {buffer.Pointer}");
            EvaluatePhase(evaluated, buffer);

            Debug.WriteLine("Done Evaluating. ");
            Debug.WriteLine($"\tEvaluated Pointer: {evaluated.Pointer}");
            Debug.WriteLine($"\tBuffer Pointer: {buffer.Pointer}");
        }

        private void EvaluatePhase(PointerArray<T> evaluated, PointerArray<T> buffer)
        {
            BatchDecode<T> llamaBatch = new();

            for(uint i = 0; i < buffer.Pointer; i++)
            {
                if (Equals(buffer[i], _defaultT))
                {
                    break;
                }

                if (!Equals(evaluated[i], buffer[i]))
                {
                    llamaBatch.AddItem(buffer[i], i);
                }

                if(llamaBatch.Items.Count == _batchSize)
                {
                    Debug.WriteLine($"Evaluating: {llamaBatch.Items.Count}");

                    _arrayShifter.Decode(llamaBatch);

                    llamaBatch = new BatchDecode<T>();
                }
            }

            if (llamaBatch.Items.Count > 0)
            {
                Debug.WriteLine($"Evaluating: {llamaBatch.Items.Count}");

                _arrayShifter.Decode(llamaBatch);
            }

            evaluated.Pointer = buffer.Pointer;

            //The entirety of the token data needs to be synced for all tokens regardless
            //once the eval is complete, because otherwise metadata wont be copied across
            //The copy call above only intends on copying for the sake of the modification
            //event but an additional "full sync" call is needed.
            for (uint i = 0; i < evaluated.Pointer; i++)
            {
                evaluated[i] = buffer[i];
            }
        }

        private void ShiftPhase(PointerArray<T> evaluated, PointerArray<T> buffer)
        {
            ShiftItem[] shiftItems = new ShiftItem[evaluated.Length];

            int j = 0;

            for (uint i = 0; i < evaluated.Pointer; i++)
            {
                ShiftItem shiftToken = new()
                {
                    Item = evaluated[i],
                    OriginalIndex = i
                };

                int start_j = j;

                do
                {
                    if (shiftItems[j] == null && Equals(buffer[(uint)j], shiftToken.Item))
                    {
                        shiftItems[j] = shiftToken;
                        shiftToken.NewIndex = (uint)j++;
                        break;
                    }

                    if (++j == evaluated.Length)
                    {
                        j = 0;
                    }
                } while (j != start_j);
            }

            for (int i = 0; i < evaluated.Length; i++)
            {
                ShiftItem item = shiftItems[i];

                if ((item?.Delta ?? 0) != 0)
                {
                    _arrayShifter.ShiftCacheToken(0, item!.OriginalIndex, item.Delta);
                    evaluated[item.NewIndex] = item.Item;
                }
            }

            for (uint i = 0; i < evaluated.Length; i++)
            {
                ShiftItem item = shiftItems[i];

                if (item is null && !Equals(evaluated[i], _defaultT))
                {
                    _arrayShifter.RemoveCacheToken(i);
                    evaluated[i] = _defaultT;
                }
            }
        }

        private class ShiftItem
        {
            public int Delta => (int)(NewIndex - OriginalIndex);

            public T Item { get; set; }

            public uint NewIndex { get; set; }

            public uint OriginalIndex { get; set; }
        }
    }
}