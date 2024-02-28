namespace raft;
public class Gateway
{
  private readonly List<RaftNode> nodes;
  private readonly Random random = new();

  public Gateway(List<RaftNode> nodes)
  {
    this.nodes = nodes;
  }

  private RaftNode? FindLeader()
  {
    var leader = nodes.FirstOrDefault(n => n.Id == RaftNode.MostRecentLeaderId);
    return leader;
  }

  public int? EventualGet(string key)
  {
    var node = nodes[random.Next(nodes.Count)];
    return node.DataLog.TryGetValue(key, out var value) ? value.value : null;
  }

  public int? StrongGet(string key)
  {
    var leader = FindLeader();
    if (leader != null && leader.State == NodeState.Leader)
    {
      return leader.DataLog.TryGetValue(key, out var value) ? value.value : null;
    }
    return null;
  }

  public bool CompareVersionAndSwap(string key, int expectedValue, int newValue)
  {
    var leader = FindLeader();
    if (leader != null && leader.DataLog.TryGetValue(key, out var value) && value.value == expectedValue)
    {
      var newLogIndex = leader.LogEntries.Max(e => e.LogIndex) + 1;
      leader.AppendEntry(new LogEntry { Key = key, Value = newValue, LogIndex = newLogIndex, Term = leader.CurrentTerm });
      return true;
    }
    return false;
  }

  public bool Write(string key, int value)
  {
    var leader = FindLeader();
    if (leader != null)
    {
      var newLogIndex = leader.LogEntries.Count != 0 ? leader.LogEntries.Max(e => e.LogIndex) + 1 : 0;
      var newLogEntry = new LogEntry
      {
        LogIndex = newLogIndex,
        Key = key,
        Value = value,
        Term = leader.CurrentTerm
      };

      leader.AppendEntry(newLogEntry);

      return true;
    }
    return false;
  }
}
