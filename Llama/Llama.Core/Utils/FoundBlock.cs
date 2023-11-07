namespace Llama.Core.Utils
{
    internal partial class KvCacheShifter
    {
        public class FoundBlock
        {
            public uint Offset = 0;

            public uint Size = 0;

            public List<TokenReplacement> TokenReplacements { get; set; } = new();

            public void AddReplacement(int pos, int value)
            {
                if (pos < 0)
                {
                    throw new ArgumentException("Position must be >= 0");
                }

                TokenReplacements.Add(new TokenReplacement((uint)pos, value));
            }
        }
    }
}