namespace UserSummarizer
{
    public class Embedding
    {
        public string Content { get; set; }

        public double[] Data { get; set; }

        public DateTime DateCreated { get; set; }

        public override string ToString() => this.Content;
    }
}