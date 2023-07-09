namespace UserSummarizer
{
    public class EmbeddingDistance
    {
        public double Distance { get; set; }

        public Embedding Embedding { get; set; }

        public override string ToString() => $"[{this.Distance:0.####}] {this.Embedding}";
    }
}