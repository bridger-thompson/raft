using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

enum NodeState { Follower, Candidate, Leader }

class RaftNode
{
  public Guid Id { get; private set; }
  public NodeState State { get; set; }
  public int CurrentTerm { get; set; }

  private readonly static List<RaftNode> allNodes = [];
  private readonly static Dictionary<Guid, (int, Guid?)> votesRecord = [];
  private readonly object lockObject = new();

  private readonly Random random = new();
  private int electionTimeout;

  public RaftNode()
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;

    allNodes.Add(this);
    votesRecord[Id] = (0, null);
    ResetElectionTimeout();
  }

  private void ResetElectionTimeout()
  {
    electionTimeout = random.Next(150, 300);
  }

  public void Run()
  {
    while (true)
    {
      Thread.Sleep(electionTimeout);
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
  }

  private void StartElection()
  {
    Log("Started election.");
    CurrentTerm++;
    int voteCount = 1;
    votesRecord[Id] = (CurrentTerm, Id);

    lock (lockObject)
    {
      foreach (var node in allNodes)
      {
        if (node.Id != Id)
        {
          if (node.CurrentTerm <= CurrentTerm && (!votesRecord[node.Id].Item2.HasValue || votesRecord[node.Id].Item1 < CurrentTerm))
          {
            voteCount++;
            Log($"Received vote from {node.Id}");
            if (voteCount > allNodes.Count / 2)
            {
              State = NodeState.Leader;
              Log("Became the leader");
              SendHeartbeat();
              return;
            }
          }
        }
      }
    }
    Log("Lost election.");
    State = NodeState.Follower;
  }

  private void Follow()
  {
    State = NodeState.Candidate;
  }

  private void SendHeartbeat()
  {
    Log("Sending heartbeat as leader");

    foreach (var node in allNodes)
    {
      if (node.Id != Id)
      {
        node.ReceiveHeartbeat(CurrentTerm);
      }
    }
  }

  public void ReceiveHeartbeat(int termFromLeader)
  {
    CurrentTerm = termFromLeader;
    State = NodeState.Follower;
    Log($"Follower. Received heartbeat from leader with term {termFromLeader}");

    ResetElectionTimeout();
  }

  private void Log(string message)
  {
    string filename = $"{Id}.log";
    File.AppendAllText(filename, $"{DateTime.Now}: {message}\n");
  }
}

class Raft
{
  static void Main()
  {
    var nodes = new RaftNode[3];
    for (int i = 0; i < nodes.Length; i++)
    {
      nodes[i] = new RaftNode();
    }

    foreach (var node in nodes)
    {
      var thread = new Thread(new ThreadStart(node.Run));
      thread.Start();
    }
  }
}
