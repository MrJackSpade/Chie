using Microsoft.Extensions.Logging;

namespace Logging.Shared.Extensions
{
	public static class ILoggerExtensions
	{
		public static void LogError(this ILogger logger, Exception exception) => logger.LogError(exception.ToString());
	}
}