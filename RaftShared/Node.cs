using System.Text;
using System.Text.Json;
using System.Timers;

namespace RaftShared;

public class Node
{
  public Guid Id { get; private set; }
  public NodeState State { get; set; }
  public int CurrentTerm { get; set; }

  private List<string> nodeUrls;
  private int nodeCount;
  private readonly static Dictionary<Guid, (int, Guid?)> votesRecord = [];

  private readonly Random random = new();
  private readonly HttpClient httpClient;
  private int electionTimeout;

  public List<LogEntry> LogEntries { get; private set; } = [];
  public static Guid? MostRecentLeaderId { get; set; }
  public Dictionary<string, (int value, int logIndex)> DataLog = [];
  private int lastLogIndex = 0;
  private readonly System.Timers.Timer _actionTimer;


  public Node(List<string> nodeUrls)
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;
    this.nodeUrls = nodeUrls;
    nodeCount = nodeUrls.Count + 1;

    votesRecord[Id] = (CurrentTerm, null);
    ResetElectionTimeout();
    this.httpClient = new HttpClient();
    _actionTimer = new System.Timers.Timer(electionTimeout);
    _actionTimer.Elapsed += Act;
    _actionTimer.Start();
  }

  private void ResetElectionTimeout()
  {
    electionTimeout = random.Next(500, 1000);
  }

  public async void Act(object? sender, ElapsedEventArgs e)
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

    var voteTasks = new List<Task<bool>>();

    foreach (var nodeUrl in nodeUrls)
    {
      voteTasks.Add(RequestVoteFromNode(nodeUrl, CurrentTerm, Id));
    }

    var voteResults = await Task.WhenAll(voteTasks);

    foreach (var voteGranted in voteResults)
    {
      if (voteGranted)
      {
        voteCount++;
      }
    }

    if (voteCount > nodeUrls.Count / 2)
    {
      State = NodeState.Leader;
      MostRecentLeaderId = Id;
      Log("Became the leader");
      await SendHeartbeatAsync();
    }
    else
    {
      Log("Lost election. Still candidate.");
    }
  }

  private async Task<bool> RequestVoteFromNode(string nodeUrl, int term, Guid candidateId)
  {
    var request = new VoteRequest
    {
      Term = term,
      CandidateId = candidateId
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await httpClient.PostAsync($"{nodeUrl}/RaftNode/vote", content);
      if (response.IsSuccessStatusCode)
      {
        var responseString = await response.Content.ReadAsStringAsync();
        var voteResponse = JsonSerializer.Deserialize<VoteResponse>(responseString); // Assuming a VoteResponse DTO
        return voteResponse?.VoteGranted ?? false;
      }
    }
    catch (Exception ex)
    {
      Log($"Failed to request vote from {nodeUrl}: {ex.Message}");
    }
    return false;
  }

  public bool Vote(int term, Guid id)
  {
    votesRecord[Id] = (term, id);
    Log($"Voted for node {id} for term {term}");
    return true;
  }

  public void Follow()
  {
    State = NodeState.Candidate;
  }

  public async Task SendHeartbeatAsync()
  {
    Log("Sending heartbeat as leader");
    if (State != NodeState.Leader) return;

    var tasks = new List<Task>();
    foreach (var nodeUrl in nodeUrls)
    {
      var entriesToReplicate = LogEntries;
      tasks.Add(SendAppendEntriesAsync(nodeUrl, CurrentTerm, Id, entriesToReplicate));
    }

    await Task.WhenAll(tasks);
  }

  private async Task SendAppendEntriesAsync(string nodeUrl, int term, Guid leaderId, List<LogEntry> entries)
  {
    var request = new AppendEntriesRequest
    {
      Term = term,
      LeaderId = leaderId,
      Entries = entries
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await httpClient.PostAsync($"{nodeUrl}/RaftNode/appendEntries", content);
      if (!response.IsSuccessStatusCode)
      {
        Log($"Failed to send append entries to {nodeUrl}. Code {response.StatusCode}");
      }
    }
    catch (Exception ex)
    {
      Log($"Exception sending append entries to {nodeUrl}: {ex.Message}");
    }
  }

  private void Log(string message)
  {
    string filename = $"{Id}.log";
    File.AppendAllText(filename, $"{DateTime.Now}: {message}\n");
    Console.WriteLine(message);
  }

  public void RecordLog(LogEntry entry)
  {
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

      LogEntries = entries;
      foreach (var entry in entries)
      {
        RecordLog(entry);
      }
      ResetElectionTimeout();
    }
  }

  public bool IsLeader() => State == NodeState.Leader;

  public (int? value, int logIndex) EventualGet(string key)
  {
    if (DataLog.TryGetValue(key, out var data))
    {
      return (data.value, data.logIndex);
    }
    return (null, 0);
  }

  public (int? value, int logIndex) StrongGet(string key)
  {
    if (!IsLeader()) return (null, 0);

    if (DataLog.TryGetValue(key, out var data))
    {
      return (data.value, data.logIndex);
    }
    return (null, 0);
  }

  public bool CompareVersionAndSwap(string key, int expectedValue, int newValue, int expectedLogIndex)
  {
    if (!IsLeader()) return false;

    if (DataLog.TryGetValue(key, out var data) && data.value == expectedValue && data.logIndex == expectedLogIndex)
    {
      DataLog[key] = (newValue, ++lastLogIndex);
      return true;
    }
    return false;
  }

  public bool Write(string key, int value)
  {
    if (!IsLeader()) return false;

    if (DataLog.ContainsKey(key))
    {
      DataLog[key] = (value, DataLog[key].logIndex + 1);
    }
    else
    {
      DataLog.Add(key, (value, ++lastLogIndex));
    }
    return true;
  }
}