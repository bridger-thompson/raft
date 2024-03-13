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
  private readonly Gateway raftGateway;

  public RaftGatewayController(Gateway raftGateway)
  {
    this.raftGateway = raftGateway;
  }

  [HttpGet("EventualGet")]
  public async Task<ActionResult> EventualGet(string key)
  {
    var result = await raftGateway.EventualGetAsync(key);
    if (result.HasValue)
    {
      var (value, logIndex) = result.Value;
      return Ok(new { value, logIndex });
    }
    else
    {
      return NotFound("No value found for the key.");
    }
  }

  [HttpGet("StrongGet")]
  public async Task<ActionResult> StrongGet(string key)
  {
    var result = await raftGateway.StrongGetAsync(key);
    if (result.HasValue)
    {
      var (value, logIndex) = result.Value;
      return Ok(new { value, logIndex });
    }
    else
    {
      return NotFound("No value");
    }
  }

  [HttpPost("CompareVersionAndSwap")]
  public async Task<ActionResult<bool>> CompareVersionAndSwap(string key, int expectedValue, int newValue)
  {
    var result = await raftGateway.CompareVersionAndSwapAsync(key, expectedValue, newValue);
    return Ok(result);
  }

  [HttpPost("Write")]
  public async Task<ActionResult<bool>> Write(string key, int value)
  {
    var result = await raftGateway.WriteAsync(key, value);
    return Ok(result);
  }
}
