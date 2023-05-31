namespace ChieApi.Tasks.Boredom
{
	public class BoredomTaskData
	{
		public DateTime LastMessage { get; set; } = DateTime.MinValue;

		public Dictionary<string, int> MessageCounts { get; set; } = new();
	}
}