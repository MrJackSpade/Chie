namespace Llama.Data.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class BatchDecode<T>
    {
        public float[] Embeddings { get; set; }

        public List<BatchItem<T>> Items { get; set; } = new List<BatchItem<T>>();

        public byte[] Logits { get; set; }

        public void AddItem(T token, uint position, int[] sequenceIds = null, bool includeLogits = false)
        {
            BatchItem<T> item = new()
            {
                SequenceIds = sequenceIds ?? new int[] { 0 },
                Position = position,
                Token = token,
                IncludeLogits = includeLogits
            };

            Items.Add(item);
        }
    }

    public class BatchItem<T>
    {
        public bool IncludeLogits { get; set; }

        public uint Position { get; set; }

        public int[] SequenceIds { get; set; }

        public T Token { get; set; }
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}