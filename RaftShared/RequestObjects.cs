

namespace RaftShared;

public class VoteRequest
{
  public int Term { get; set; }
  public Guid CandidateId { get; set; }
}

public class AppendEntriesRequest
{
  public int Term { get; set; }
  public Guid LeaderId { get; set; }
  public List<LogEntry> Entries { get; set; } = [];
}

public class HeartbeatRequest
{
  public int Term { get; set; }
  public Guid LeaderId { get; set; }
}

public class VoteResponse
{
  public bool VoteGranted { get; set; }
}
