using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace raft;

public enum NodeState { Follower, Candidate, Leader }

public class LogEntry
{
  public int LogIndex { get; set; }
  public string Key { get; set; } = string.Empty;
  public int Value { get; set; }
  public int Term { get; set; }
}

public class RaftNode
{
  public Guid Id { get; private set; }
  public NodeState State { get; set; }
  public int CurrentTerm { get; set; }

  private readonly static List<RaftNode> allNodes = [];
  private readonly static Dictionary<Guid, (int, Guid?)> votesRecord = [];
  private readonly static object lockObject = new();

  private readonly Random random = new();
  private int electionTimeout;
  private bool _healthy;

  public List<LogEntry> LogEntries { get; private set; } = [];
  public static Guid? MostRecentLeaderId { get; set; }
  public Dictionary<string, (int value, int logIndex)> DataLog = [];


  public RaftNode(bool healthy = true)
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;

    lock (lockObject)
    {
      allNodes.Add(this);
    }
    votesRecord[Id] = (CurrentTerm, null);
    ResetElectionTimeout();
    _healthy = healthy;
  }

  private void ResetElectionTimeout()
  {
    electionTimeout = random.Next(150, 300);
  }

  public void Run()
  {
    while (_healthy)
    {
      Log($"Waiting for {electionTimeout}ms");
      Thread.Sleep(electionTimeout);
      Act();
    }
  }

  public void Act()
  {
    if (_healthy == false) return;
    switch (State)
    {
      case NodeState.Follower:
        Follow();
        break;
      case NodeState.Candidate:
        StartElection();
        break;
      case NodeState.Leader:
        SendHeartbeat();
        break;
    }
  }

  public bool IsHealthy()
  {
    return _healthy;
  }

  public void Reboot()
  {
    State = NodeState.Follower;
    _healthy = true;
    ResetElectionTimeout();
  }

  public void Die()
  {
    _healthy = false;
  }

  public void StartElection()
  {
    Log("Started election.");
    CurrentTerm++;
    int voteCount = 1;
    votesRecord[Id] = (CurrentTerm, Id);
    lock (lockObject)
    {
      // get votes
      foreach (var node in allNodes)
      {
        if (node.Id != Id)
        {
          if (node.CurrentTerm <= CurrentTerm &&
            node.IsHealthy() &&
            (!votesRecord[node.Id].Item2.HasValue || votesRecord[node.Id].Item1 < CurrentTerm)
          )
          {
            node.Vote(CurrentTerm, Id);
            voteCount++;
            Log($"Received vote from {node.Id} for term {CurrentTerm}");
          }
        }
      }
      if (voteCount > allNodes.Count / 2)
      {
        State = NodeState.Leader;
        Log("Became the leader");
        SendHeartbeat();
        return;
      }
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

  public void SendHeartbeat()
  {
    Log("Sending heartbeat as leader");

    lock (lockObject)
    {
      foreach (var node in allNodes.Where(n => n.Id != Id))
      {
        var entriesToReplicate = LogEntries.Where(e => !node.LogEntries.Select(le => le.LogIndex).Contains(e.LogIndex)).ToList();
        node.ReceiveAppendEntries(CurrentTerm, Id, entriesToReplicate);
      }
    }
    ResetElectionTimeout();
  }

  private void Log(string message)
  {
    string filename = $"{Id}.log";
    File.AppendAllText(filename, $"{DateTime.Now}: {message}\n");
  }

  public static void ResetState()
  {
    allNodes.Clear();
    votesRecord.Clear();
  }

  public void AppendEntry(LogEntry entry)
  {
    LogEntries.Add(entry);
    DataLog[entry.Key] = (entry.Value, entry.LogIndex);
    Log($"Appended log entry: {entry.Key} = {entry.Value}");
  }

  public void ReceiveAppendEntries(int term, Guid leaderId, List<LogEntry> entries)
  {
    if (!_healthy) return;

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

class Raft
{
  static void Main()
  {
    var nodes = new RaftNode[6];
    for (int i = 0; i < nodes.Length; i++)
    {
      nodes[i] = new RaftNode(true);
    }

    foreach (var node in nodes)
    {
      var thread = new Thread(new ThreadStart(node.Run));
      thread.Start();
    }

    Gateway gateway = new(new(nodes));

    var count = 0;
    while (true)
    {
      bool writeSuccess = gateway.Write("someKey", count);
      var readValue = gateway.EventualGet("someKey");
      var readValueStrong = gateway.StrongGet("someKey");
      var casResult = gateway.CompareVersionAndSwap("someKey", expectedValue: count, newValue: count + 1);

      Console.WriteLine($"Read (Eventual): {readValue}");
      Console.WriteLine($"Read (Strong): {readValueStrong}");
      Console.WriteLine($"CAS Result: {casResult}\n");

      count++;
      Thread.Sleep(500);
    }
  }
}
