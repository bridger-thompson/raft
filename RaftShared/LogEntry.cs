namespace RaftShared;

public class LogEntry
{
  public int LogIndex { get; set; }
  public string Key { get; set; } = string.Empty;
  public string Value { get; set; } = string.Empty;
  public int Term { get; set; }
}