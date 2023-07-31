namespace ChieApi.Models
{
    public class WordDrift
    {
        public WordDrift(string wordA, string wordB, int drift)
        {
            this.WordA = wordA;
            this.WordB = wordB;
            this.Drift = drift;
        }

        public string WordA { get; set; }
        public string WordB { get; set; }
        public int Drift { get; set; } 
    }
}
