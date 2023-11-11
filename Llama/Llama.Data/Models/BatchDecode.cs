namespace Llama.Data.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class BatchDecode<T>
    {
        private List<BatchItem<T>> _items = new();

        public int Count => _items.Count;

        public float[] Embeddings { get; set; }

        public IReadOnlyList<BatchItem<T>> Items => this._items;

        public byte[] Logits { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="token"></param>
        /// <param name="position"></param>
        /// <param name="sequenceIds">Defaults to { 0 }</param>
        /// <param name="includeLogits">Defaults to false</param>
        public void AddItem(T token, uint position, int[] sequenceIds = null, bool includeLogits = false)
        {
            BatchItem<T> item = new()
            {
                SequenceIds = sequenceIds ?? new int[] { 0 },
                Position = position,
                Token = token,
                IncludeLogits = includeLogits
            };

            AddItem(item);
        }

        public void AddItem(BatchItem<T> item)
        {
            this._items.Add(item);
            this._items = this._items.OrderBy(i => i.Position).ToList();
        }

        public void Clear()
        {
            _items.Clear();
        }

        public BatchDecode<T> Clone(Func<BatchItem<T>, bool> predicate)
        {
            BatchDecode<T> result = new()
            {
                Embeddings = this.Embeddings,
                Logits = this.Logits
            };

            foreach (var item in this.Items.Where(predicate))
            {
                result.AddItem(item);
            }

            return result;
        }

        public bool TryRemove(uint positionToRemove, out BatchItem<T> found)
        {
            for(int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Position == positionToRemove)
                {
                    found = _items[i];

                    _items.RemoveAt(i);

                    return true;
                }
            }

            found = null;

            return false;   
        }
    }

    public class BatchItem<T>
    {
        public BatchItem()
        {
        }

        public BatchItem(T token, uint pos, int[]? seqIds = null)
        {
            Token = token;
            Position = pos;
            SequenceIds = seqIds ?? new int[] { 0 };
        }

        public bool IncludeLogits { get; set; }

        public uint Position { get; set; }

        public int[] SequenceIds { get; set; }

        public T Token { get; set; }

        public override string ToString()
        {
            return $"[{Position}] {Token}";
        }
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}