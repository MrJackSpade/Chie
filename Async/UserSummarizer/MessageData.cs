namespace UserSummarizer
{
    public class MessageData
    {
        public int Id { get; set; }
        public string Content { get; set; }

        public override string ToString() => $"[{this.Id}] {this.Content}";
    }
}
