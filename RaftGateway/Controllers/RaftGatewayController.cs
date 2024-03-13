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
  public async Task<ActionResult<int?>> EventualGet(string key)
  {
    var value = await raftGateway.EventualGetAsync(key);
    if (value.HasValue)
      return Ok(value.Value);
    else
      return NotFound();
  }

  [HttpGet("StrongGet")]
  public async Task<ActionResult<int?>> StrongGet(string key)
  {
    var value = await raftGateway.StrongGetAsync(key);
    if (value.HasValue)
      return Ok(value.Value);
    else
      return NotFound();
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
