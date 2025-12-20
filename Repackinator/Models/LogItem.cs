namespace Repackinator.Models
{
    public class LogItem
    {
        public string Time { get; set; } = "00:00:00";
        public string  Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
