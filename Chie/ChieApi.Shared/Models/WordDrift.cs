namespace ChieApi.Shared.Models
{
	public class WordDrift
	{
		public WordDrift(string wordA, string wordB, int drift)
		{
			this.WordA = wordA;
			this.WordB = wordB;
			this.Drift = drift;
		}

		public int Drift { get; set; }

		public string WordA { get; set; }

		public string WordB { get; set; }
	}
}