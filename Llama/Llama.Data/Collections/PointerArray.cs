using System.Collections;

namespace Llama.Data.Collections
{
    public class PointerArray<T> : IEnumerable<T>
    {
        private readonly T[] _backingData;

        public PointerArray(uint length, params T[] array)
        {
            _backingData = new T[length];

            for (int i = 0; i < array.Length; i++)
            {
                _backingData[i] = array[i];
            }
        }

        public int Count => (int)this.Pointer;

        public uint Length => (uint)_backingData.Length;

        public uint Pointer { get; set; }

        public T this[uint index]
        {
            get => _backingData[index];
            set => _backingData[index] = value;
        }

        public void Clear()
        {
            Pointer = 0;
        }

        public void Fill(T item)
        {
            for (int i = 0; i < _backingData.Length; i++)
            {
                _backingData[i] = item;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_backingData).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _backingData.GetEnumerator();
        }

        public Span<T> Slice(int startIndex, int length)
        {
            return _backingData.AsSpan().Slice(startIndex, length);
        }

        public void Slide(uint v)
        {
            for (uint i = v; i < _backingData.Length - v; i++)
            {
                _backingData[i - v] = _backingData[i];
            }

            this.Pointer -= v;
        }

        public void Slide(uint start, uint count)
        {
            for (uint i = start; i < _backingData.Length - count; i++)
            {
                _backingData[i - count] = _backingData[i];
            }

            this.Pointer -= count;
        }

        public void Write(T element)
        {
            this[this.Pointer] = element;
            this.Pointer++;
        }
    }
}