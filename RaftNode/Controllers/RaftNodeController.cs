using Microsoft.AspNetCore.Mvc;
using RaftShared;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RaftNode.Controllers;

[ApiController]
[Route("[controller]")]
public class RaftNodeController : ControllerBase
{
  private readonly Node raftNode;
  private readonly ILogger<RaftNodeController> logger;

  public RaftNodeController(Node raftNode, ILogger<RaftNodeController> logger)
  {
    this.raftNode = raftNode;
    this.logger = logger;
  }

  [HttpGet("status")]
  public IActionResult GetStatus()
  {
    return Ok(new
    {
      raftNode.Id,
      State = raftNode.State.ToString(),
      raftNode.CurrentTerm,
    });
  }

  [HttpPost("vote")]
  public IActionResult RequestVote([FromBody] VoteRequest request)
  {
    try
    {
      var voteGranted = raftNode.Vote(request.Term, request.CandidateId);
      return Ok(new { VoteGranted = voteGranted });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error requesting vote");
      return StatusCode(500, "Internal server error while processing vote request.");
    }
  }

  [HttpPost("appendEntries")]
  public IActionResult AppendEntries([FromBody] AppendEntriesRequest request)
  {
    try
    {
      Console.WriteLine($"Appending Entries: {request.Entries.Count}");
      int lastLogIndexAppended = raftNode.ReceiveAppendEntries(request.Term, request.LeaderId, request.Entries);
      return Ok(lastLogIndexAppended);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error appending entries");
      return StatusCode(500, "Internal server error while processing append entries request.");
    }
  }

  [HttpGet("leader")]
  public ActionResult<string> GetLeader()
  {
    var isLeader = raftNode.IsLeader();
    Console.WriteLine($"Is Leader: {isLeader}");
    return Ok(isLeader);
  }

  [HttpGet("eventualget")]
  public ActionResult<Data> EventualGet(string key)
  {
    var data = raftNode.EventualGet(key);
    Console.WriteLine($"Key {key} got value {data.Value} {data.LogIndex}");
    return data;
  }

  [HttpGet("strongget")]
  public ActionResult<Data> StrongGet(string key)
  {
    var data = raftNode.StrongGet(key);
    Console.WriteLine($"Key {key} got value {data.Value} {data.LogIndex}");
    return data;
  }

  [HttpPost("compareversionandswap")]
  public ActionResult<bool> CompareVersionAndSwap([FromForm] string key, [FromForm] string expectedValue, [FromForm] string newValue, [FromForm] int expectedLogIndex)
  {
    var success = raftNode.CompareVersionAndSwap(key, expectedValue, newValue, expectedLogIndex);
    return Ok(success);
  }

  [HttpPost("write")]
  public ActionResult<bool> Write([FromBody] WriteModel model)
  {
    if (model == null) return BadRequest("Invalid request payload.");

    var success = raftNode.Write(model.Key, model.Value);
    if (success)
    {
      return Ok(true);
    }
    return BadRequest("Not the leader or operation failed.");
  }
}
