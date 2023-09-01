using Repackinator.Localization.Language;

namespace Repackinator.Logging
{
    public enum LogMessageLevel
    {
        None,
        Info,
        Completed,
        Skipped,
        Warning,
        NotFound,
        Error,
        Done
    }

    public class LogMessage
    {
        public DateTime Time { get; set; }

        public string LogLevel => UserLocale.ResourceManager.GetString($"logger.level.{Level.ToString().ToLower()}") ?? $"{UserLocale.logger_level_unknown}({Level.ToString()})";

        public LogMessageLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public string ToLogFormat(string timeFormat = "HH:mm:ss")
        {
            if (Level == LogMessageLevel.None)
            {
                return "\n";
            }
            return $"{Time.ToString(timeFormat)} {LogLevel} - {Message}\n";
        }

        public LogMessage(LogMessageLevel level, string message)
        {
            Time = DateTime.Now;
            Level = level;
            Message = message;
        }
    }
}
