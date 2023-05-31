namespace ChieApi.Interfaces
{
	public interface IBackgroundTask
	{
		Task Initialize();

		Task TickMinute();
	}
}