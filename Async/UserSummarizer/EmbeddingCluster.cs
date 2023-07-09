namespace UserSummarizer
{
    public class EmbeddingCluster
    {
        public Embedding[] Embeddings = Array.Empty<Embedding>();

        public double[] Centeroid { get; set; }
    }
}