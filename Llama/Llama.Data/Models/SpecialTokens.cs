namespace Llama.Data.Models
{
    public class SpecialTokens
    {
        public int BOS { get; set; } = 1;

        public int EOS { get; set; } = 2;

        public int NewLine { get; set; } = 13;

        public int Null { get; set; } = -1;
    }
}