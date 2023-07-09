namespace Llama.Pipeline.Summarizers
{
    public partial class ChatSummarizer
    {
        private class BlockRestriction
        {
            public string[] BanTags { get; set; } = new string[0];
            public int Index { get; set; }
        }
    }
}