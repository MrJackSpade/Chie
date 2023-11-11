namespace Llama.Data.Models
{
    public class FoundBlock
    {
        public uint Offset = 0;

        public uint RequestedSize = 0;

        public uint ActualSize => (uint)(TokenReplacements.Count + RequestedSize);

        public Queue<TokenReplacement> TokenReplacements { get; set; } = new();

        public void AddReplacement(int pos, int value)
        {
            if (pos < 0)
            {
                throw new ArgumentException("Position must be >= 0");
            }

            TokenReplacements.Enqueue(new TokenReplacement((uint)pos, value));
        }
    }
}