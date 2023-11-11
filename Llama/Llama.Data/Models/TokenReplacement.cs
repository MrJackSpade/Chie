namespace Llama.Data.Models
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