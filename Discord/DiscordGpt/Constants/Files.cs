namespace DiscordGpt.Constants
{
	public static class Files
	{
		private static readonly string _exePath = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;

		public static string TYPING_GIF => new FileInfo(Path.Combine(_exePath, "typing.gif")).FullName;
	}
}