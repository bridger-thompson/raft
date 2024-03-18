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
  public async Task<ActionResult<Data>> EventualGet(string key)
  {
    var result = await raftGateway.EventualGetAsync(key);
    if (result.HasValue)
    {
      return Ok(result.Value);
    }
    else
    {
      return NotFound("No value found for the key.");
    }
  }

  [HttpGet("StrongGet")]
  public async Task<ActionResult<Data>> StrongGet(string key)
  {
    var result = await raftGateway.StrongGetAsync(key);
    if (result.HasValue)
    {
      return Ok(result.Value);
    }
    else
    {
      return NotFound("No value found for the key.");
    }
  }

  [HttpPost("CompareVersionAndSwap")]
  public async Task<ActionResult<bool>> CompareVersionAndSwap(string key, int expectedValue, int newValue, int expectedLogIndex)
  {
    var result = await raftGateway.CompareVersionAndSwapAsync(key, expectedValue, newValue, expectedLogIndex);
    return Ok(result);
  }

  [HttpPost("Write")]
  public async Task<ActionResult<bool>> Write(string key, int value)
  {
    var result = await raftGateway.WriteAsync(key, value);
    return Ok(result);
  }
}
