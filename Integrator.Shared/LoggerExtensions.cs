using Microsoft.Extensions.Logging;

namespace Integrator.Shared
{
    public static class LoggerExtensions
    {
        public static void LogWarningIf(this ILogger logger, bool condition, string message)
        {
            if (condition)
            {
                logger.LogWarning(message);
            }
        }

        public static void LogInformationIf(this ILogger logger, bool condition, string message)
        {
            if (condition)
            {
                logger.LogInformation(message);
            }
        }

        public static void LogErrorIf(this ILogger logger, bool condition, string message)
        {
            if (condition)
            {
                logger.LogError(message);
            }
        }
    }
}
