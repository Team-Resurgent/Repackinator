namespace Repackinator.Core.Logging
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

        public string LogLevel => Level switch
        {
            LogMessageLevel.None => "None",
            LogMessageLevel.Info => "Info",
            LogMessageLevel.Completed => "Completed",
            LogMessageLevel.Skipped => "Skipped",
            LogMessageLevel.Warning => "Warning",
            LogMessageLevel.NotFound => "NotFound",
            LogMessageLevel.Error => "Error",
            LogMessageLevel.Done => "Done",
            _ => $"UNKNOWN({Level.ToString()})"
        };

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
