using System.Text;
using System.Text.Json;

namespace RaftShared;

public class RaftNode
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


  public RaftNode(HttpClient httpClient, List<string> nodeUrls)
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;
    this.nodeUrls = nodeUrls;
    nodeCount = nodeUrls.Count + 1;

    votesRecord[Id] = (CurrentTerm, null);
    ResetElectionTimeout();
    this.httpClient = httpClient;
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

    if (State != NodeState.Leader) return;

    var tasks = new List<Task>();
    foreach (var nodeUrl in nodeUrls)
    {
      tasks.Add(SendHeartbeatToNodeAsync(nodeUrl));
    }

    await Task.WhenAll(tasks);

    ResetElectionTimeout();
  }

  private async Task SendHeartbeatToNodeAsync(string nodeUrl)
  {
    var request = new HeartbeatRequest
    {
      Term = this.CurrentTerm,
      LeaderId = this.Id
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await httpClient.PostAsync($"{nodeUrl}/RaftNode/heartbeat", content);
      if (response.IsSuccessStatusCode)
      {
        Log($"Heartbeat successfully sent to {nodeUrl}");
      }
      else
      {
        Log($"Failed to send heartbeat to {nodeUrl}");
      }
    }
    catch (Exception ex)
    {
      Log($"Exception sending heartbeat to {nodeUrl}: {ex.Message}");
    }
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