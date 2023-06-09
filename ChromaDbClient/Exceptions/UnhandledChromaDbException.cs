namespace ChromaDbClient.Exceptions
{
	internal class UnhandledChromaDbException : ChromaDbException
	{
		public UnhandledChromaDbException(string error, string? message) : base($"{error}: {message}")
		{
			this.Error = error ?? string.Empty;
			this.Message = message ?? string.Empty;
		}

		public string Error { get; private set; }
		public string Message { get; private set; }
	}
}
