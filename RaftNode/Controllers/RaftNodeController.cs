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
      raftNode.ReceiveAppendEntries(request.Term, request.LeaderId, request.Entries);
      return Ok(new { Success = true });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error appending entries");
      return StatusCode(500, "Internal server error while processing append entries request.");
    }
  }
}
