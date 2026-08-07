namespace Polootyyy.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
}

public enum LogLevel { Info, Success, Warning, Error }
