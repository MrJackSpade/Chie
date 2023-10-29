using Llama.Core.Interfaces;
using Llama.Data.Collections;
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
            int start = (int)evaluated.Pointer;

            int end = (int)buffer.Pointer;

            Span<T> toEvaluate = buffer.Slice(start, end - start);

            Debug.WriteLine($"Evaluating: {toEvaluate.Length}");

            // evaluate tokens in batches
            // embed is typically prepared beforehand to fit within a batch, but not always
            for (uint i = 0; i < toEvaluate.Length; i += _batchSize)
            {
                uint n_eval = (uint)(toEvaluate.Length - i);

                if (n_eval > _batchSize)
                {
                    n_eval = _batchSize;
                }

                Span<T> thisBlock = toEvaluate.Slice((int)i, (int)n_eval);

                try
                {
                    Debug.WriteLine($"{evaluated.Pointer + 1}/{end}");

                    _arrayShifter.Evaluate(thisBlock.ToArray(), evaluated.Pointer);
                }
                catch (Exception e) when (Debugger.IsAttached)
                {
                    Debug.WriteLine(e);
                    Debugger.Break();
                }

                for (uint c = 0; c < n_eval; c++)
                {
                    uint c_loc = c + evaluated.Pointer;

                    evaluated[c_loc] = buffer[c_loc];
                }

                evaluated.Pointer += n_eval;
            }

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
            uint e_index = evaluated.Pointer;

            while (e_index < buffer.Pointer)
            {
                T toMatch = buffer[e_index];

                if (!Equals(evaluated[e_index], toMatch))
                {
                    uint s_shift = e_index + 1;

                    while (!Equals(evaluated[s_shift], toMatch))
                    {
                        s_shift++;

                        if (s_shift == evaluated.Length)
                        {
                            return;
                        }
                    }

                    uint c_shift = 1;

                    while (c_shift + s_shift < evaluated.Length && Equals(evaluated[c_shift + s_shift], buffer[e_index + c_shift]))
                    {
                        c_shift++;
                    }

                    Shift(evaluated, e_index, s_shift, c_shift);

                    evaluated.Pointer += c_shift;
                    Debug.WriteLine($"New Evaluated Pointer: {evaluated.Pointer}");
                    e_index += c_shift;
                }
                else
                {
                    evaluated.Pointer++;
                    e_index++;
                }
            }
        }

        private void Shift(PointerArray<T> container, uint e_index, uint startPos, uint shiftCount)
        {
            _arrayShifter.ShiftCacheTokens(0, startPos, startPos + shiftCount, (int)(e_index - startPos));

            for (uint i = 0; i < shiftCount; i++)
            {
                container[e_index + i] = container[startPos + i];
                container[startPos + i] = _defaultT;
            }
        }
    }
}