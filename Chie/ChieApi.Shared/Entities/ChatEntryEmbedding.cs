namespace ChieApi.Shared.Entities
{
    public class ChatEntryEmbedding
    {
        public long ChatEntryId { get; set; }

        public byte[] Data { get; set; } = Array.Empty<byte>();

        public int ModelId { get; set; }
    }
}