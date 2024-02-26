using Xunit;
using raft;
using System.Collections.Generic;
using System.Linq;

namespace RaftTests;
public class RaftNodeElectionTests : IDisposable
{
    public RaftNodeElectionTests()
    {
        RaftNode.ResetState();
    }

    public void Dispose()
    {
        RaftNode.ResetState();
    }

    [Fact]
    public void Leader_Elected_If_Two_Of_Three_Nodes_Are_Healthy()
    {
        var nodes = new List<RaftNode>
            {
                new RaftNode(true),
                new RaftNode(true),
                new RaftNode(false),
            };

        SimulateElectionProcess(nodes);

        int leadersCount = nodes.Count(n => n.State == NodeState.Leader);
        Assert.Equal(1, leadersCount);
    }

    [Fact]
    public void Leader_Elected_If_Three_Of_Five_Nodes_Are_Healthy()
    {
        var nodes = new List<RaftNode>
            {
                new RaftNode(true),
                new RaftNode(true),
                new RaftNode(true),
                new RaftNode(false),
                new RaftNode(false)
            };

        SimulateElectionProcess(nodes);

        int leadersCount = nodes.Count(n => n.State == NodeState.Leader);
        Assert.Equal(1, leadersCount);
    }

    [Fact]
    public void Leader_Not_Elected_If_Three_Of_Five_Nodes_Are_Unhealthy()
    {
        var nodes = new List<RaftNode>
            {
                new RaftNode(true),
                new RaftNode(true),
                new RaftNode(false),
                new RaftNode(false),
                new RaftNode(false)
            };

        SimulateElectionProcess(nodes);

        int leadersCount = nodes.Count(n => n.State == NodeState.Leader);
        Assert.Equal(0, leadersCount);
    }

    [Fact]
    public void Node_Continues_As_Leader_If_All_Nodes_Remain_Healthy()
    {
        var nodes = new List<RaftNode>
            {
                new RaftNode(true),
                new RaftNode(true),
                new RaftNode(true)
            };

        var initialLeader = SimulateElectionProcess(nodes);
        initialLeader.Act();

        Assert.True(initialLeader.State == NodeState.Leader);
        Assert.Equal(1, nodes.Count(n => n.State == NodeState.Leader));
        Assert.Equal(2, nodes.Count(n => n.State == NodeState.Follower));
    }

    [Fact]
    public void Node_Calls_For_Election_If_Leader_Takes_Too_Long()
    {
        var nodes = new List<RaftNode>
            {
                new RaftNode(true),
                new RaftNode(true),
                new RaftNode(true)
            };

        var initialLeader = SimulateElectionProcess(nodes);

        var follower = nodes.First(n => n != initialLeader);
        follower.Act();
        Assert.Equal(NodeState.Candidate, follower.State);
        follower.Act();
        Assert.Equal(NodeState.Leader, follower.State);
    }

    [Fact]
    public void Leader_Continues_If_Two_Of_Five_Nodes_Become_Unhealthy()
    {
        var nodes = new List<RaftNode>
        {
            new RaftNode(true),
            new RaftNode(true),
            new RaftNode(true),
            new RaftNode(true),
            new RaftNode(true)
        };

        var initialLeader = SimulateElectionProcess(nodes);
        Assert.NotNull(initialLeader);

        var nodesToBecomeUnhealthy = nodes.Where(n => n != initialLeader).Take(2).ToList();
        foreach (var node in nodesToBecomeUnhealthy)
        {
            node.Die();
        }

        foreach (var node in nodes)
        {
            node.Act();
        }

        Assert.True(initialLeader.State == NodeState.Leader, "The initial leader should continue leading.");
        Assert.Single(nodes.Where(n => n.State == NodeState.Leader), initialLeader);
    }

    [Fact]
    public void Avoids_Double_Voting_After_Reboot()
    {
        var nodes = new List<RaftNode>
        {
            new RaftNode(true), // A
            new RaftNode(true), // B
            new RaftNode(true), // C
            new RaftNode(true), // D
            new RaftNode(true)  // E
        };

        nodes[0].StartElection();

        var electedLeader = nodes.FirstOrDefault(n => n.State == NodeState.Leader);
        foreach (var node in nodes.Skip(1).Take(3))
        {
            node.Reboot();
        }

        nodes[4].CurrentTerm--;
        nodes[4].StartElection();

        // Assert: Ensure that nodes B, C, and D do not vote again in the same term after rebooting.
        Assert.NotEqual(NodeState.Leader, nodes[4].State);

        // Further assert that the original leader remains, indicating no successful reelection occurred.
        Assert.True(electedLeader.State == NodeState.Leader, "Original leader should maintain its state.");
    }


    private RaftNode SimulateElectionProcess(List<RaftNode> nodes)
    {
        foreach (var node in nodes)
        {
            node.Act(); // first act to switch from follower to candidate
            node.Act(); // second act to start election
        }
        return nodes.FirstOrDefault(n => n.State == NodeState.Leader);
    }
}