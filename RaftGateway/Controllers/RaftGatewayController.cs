using Microsoft.AspNetCore.Mvc;
using RaftShared;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RaftGateway.RaftGatewayController;

[ApiController]
[Route("[controller]")]
public class RaftGatewayController : ControllerBase
{
  private readonly RaftGateway raftGateway;

  public RaftGatewayController(RaftGateway raftGateway)
  {
    this.raftGateway = raftGateway;
  }

  [HttpGet("EventualGet")]
  public ActionResult<int?> EventualGet(string key)
  {
    var value = raftGateway.EventualGet(key);
    if (value.HasValue)
      return Ok(value.Value);
    else
      return NotFound();
  }

  [HttpGet("StrongGet")]
  public ActionResult<int?> StrongGet(string key)
  {
    var value = raftGateway.StrongGet(key);
    if (value.HasValue)
      return Ok(value.Value);
    else
      return NotFound();
  }

  [HttpPost("CompareVersionAndSwap")]
  public ActionResult<bool> CompareVersionAndSwap(string key, int expectedValue, int newValue)
  {
    var result = raftGateway.CompareVersionAndSwap(key, expectedValue, newValue);
    return Ok(result);
  }

  [HttpPost("Write")]
  public ActionResult<bool> Write(string key, int value)
  {
    var result = raftGateway.Write(key, value);
    return Ok(result);
  }
}
