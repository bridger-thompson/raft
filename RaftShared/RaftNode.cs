namespace RaftShared;

public class RaftNode
{
  public Guid Id { get; private set; }
  public NodeState State { get; set; }
  public int CurrentTerm { get; set; }

  private static List<string> nodeUrls = [];
  private int nodeCount;
  private readonly static Dictionary<Guid, (int, Guid?)> votesRecord = [];

  private readonly Random random = new();
  private int electionTimeout;

  public List<LogEntry> LogEntries { get; private set; } = [];
  public static Guid? MostRecentLeaderId { get; set; }
  public Dictionary<string, (int value, int logIndex)> DataLog = [];


  public RaftNode()
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;
    nodeUrls = [];
    nodeCount = nodeUrls.Count + 1;

    votesRecord[Id] = (CurrentTerm, null);
    ResetElectionTimeout();
  }

  private void ResetElectionTimeout()
  {
    electionTimeout = random.Next(150, 300);
  }

  public async Task Run()
  {
    Log($"Waiting for {electionTimeout}ms");
    Thread.Sleep(electionTimeout);
    await Act();
  }

  public async Task Act()
  {
    switch (State)
    {
      case NodeState.Follower:
        Follow();
        break;
      case NodeState.Candidate:
        await StartElection();
        break;
      case NodeState.Leader:
        await SendHeartbeatAsync();
        break;
    }
  }

  public async Task StartElection()
  {
    Log("Started election.");
    CurrentTerm++;
    int voteCount = 1;
    votesRecord[Id] = (CurrentTerm, Id);
    // get votes
    foreach (var node in nodeUrls)
    {
      // if (node.Id != Id)
      // {
      //   if (node.CurrentTerm <= CurrentTerm &&
      //     node.IsHealthy() &&
      //     (!votesRecord[node.Id].Item2.HasValue || votesRecord[node.Id].Item1 < CurrentTerm)
      //   )
      //   {
      //     node.Vote(CurrentTerm, Id);
      //     voteCount++;
      //     Log($"Received vote from {node.Id} for term {CurrentTerm}");
      //   }
      // }
    }
    if (voteCount > nodeCount / 2)
    {
      State = NodeState.Leader;
      Log("Became the leader");
      await SendHeartbeatAsync();
      return;
    }
    Log("Lost election. Still candidate.");
  }

  public void Vote(int term, Guid id)
  {
    votesRecord[Id] = (term, id);
    Log($"Voted for node {id} for term {term}");
  }

  public void Follow()
  {
    State = NodeState.Candidate;
  }

  public async Task SendHeartbeatAsync()
  {
    Log("Sending heartbeat as leader");

    var tasks = new List<Task<bool>>();
    int majority = (nodeCount / 2) + 1;
    int responses = 0;

    // foreach (var node in nodeUrls.Where(n => n.Id != Id))
    // {
    //   tasks.Add(Task.Run(() =>
    //   {
    //     var entriesToReplicate = LogEntries.Where(e => !node.LogEntries.Select(le => le.LogIndex).Contains(e.LogIndex)).ToList();
    //     node.ReceiveAppendEntries(CurrentTerm, Id, entriesToReplicate);
    //     return true;
    //   }));
    // }

    while (responses < majority && tasks.Count != 0)
    {
      var completedTask = await Task.WhenAny(tasks);
      tasks.Remove(completedTask);

      if (await completedTask)
      {
        responses++;
      }

      if (responses >= majority)
      {
        break;
      }
    }

    ResetElectionTimeout();
    Log($"Heartbeat acknowledged by majority of {responses} nodes.");
  }


  private void Log(string message)
  {
    string filename = $"{Id}.log";
    File.AppendAllText(filename, $"{DateTime.Now}: {message}\n");
  }

  public void AppendEntry(LogEntry entry)
  {
    LogEntries.Add(entry);
    DataLog[entry.Key] = (entry.Value, entry.LogIndex);
    Log($"Appended log entry: {entry.Key} = {entry.Value}");
  }

  public void ReceiveAppendEntries(int term, Guid leaderId, List<LogEntry> entries)
  {
    if (term >= CurrentTerm)
    {
      State = NodeState.Follower;
      CurrentTerm = term;
      MostRecentLeaderId = leaderId;
      Log($"Follower. Received {entries.Count} AppendEntries from {leaderId} with term {term}");

      foreach (var entry in entries)
      {
        AppendEntry(entry);
      }
      ResetElectionTimeout();
    }
  }
}