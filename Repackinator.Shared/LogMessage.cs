namespace Repackinator.Shared
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

        public LogMessageLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public LogMessage(LogMessageLevel level, string message)
        {
            Time = DateTime.Now;
            Level = level;
            Message = message;
        }
    }
}
