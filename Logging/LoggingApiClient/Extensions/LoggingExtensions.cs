namespace Microsoft.Extensions.Logging
{
    public static class LoggingExtensions
    {
        public static void LogInformation(this ILogger logger, string message, string data) => logger.LogInformation(message + "\x002" + data);
    }
}