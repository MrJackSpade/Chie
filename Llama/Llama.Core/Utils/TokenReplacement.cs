namespace Llama.Core.Utils
{
    internal partial class KvCacheShifter
    {
        public class TokenReplacement
        {
            public TokenReplacement(uint pos, int value)
            {
                Pos = pos;
                Value = value;
            }

            public uint Pos { get; set; }

            public int Value { get; set; }
        }
    }
}