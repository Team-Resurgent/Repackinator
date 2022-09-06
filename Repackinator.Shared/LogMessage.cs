namespace Repackinator.Shared
{
    public enum LogMessageLevel
    {
        Info,
        Warning,
        Error
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
