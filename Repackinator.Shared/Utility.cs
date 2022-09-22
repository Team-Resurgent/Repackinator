namespace Repackinator.Shared
{
    public static class Utility
    {
        public static string? GetApplicationPath()
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            if (exePath == null)
            {
                return null;
            }

            var result = Path.GetDirectoryName(exePath);
            return result;
        }

        public static string FormatLogMessage(LogMessage logMessage)
        {
            if (logMessage.Level == LogMessageLevel.None)
            {
                return "\n";
            }
            var formattedTime = logMessage.Time.ToString("HH:mm:ss");
            var message = $"{formattedTime} {logMessage.Level} - {logMessage.Message}";
            return $"{message}\n";
        }
    }
}
