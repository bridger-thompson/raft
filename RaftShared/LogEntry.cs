namespace RaftShared;

public class LogEntry
{
  public int LogIndex { get; set; }
  public string Key { get; set; } = string.Empty;
  public int Value { get; set; }
  public int Term { get; set; }
}