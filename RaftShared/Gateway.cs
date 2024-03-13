using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RaftShared
{
  public class Gateway
  {
    private readonly List<string> nodeUrls;
    private readonly HttpClient httpClient;
    private readonly ILogger<Gateway> logger;
    private readonly Random random = new();

    public Gateway(List<string> nodeUrls, ILogger<Gateway> logger)
    {
      this.nodeUrls = nodeUrls;
      this.httpClient = new HttpClient();
      this.logger = logger;
    }

    private async Task<string?> FindLeaderAsync()
    {
      foreach (var url in nodeUrls)
      {
        try
        {
          var response = await httpClient.GetAsync($"{url}/api/raftnode/leader");
          if (response.IsSuccessStatusCode)
          {
            var content = await response.Content.ReadAsStringAsync();
            var leaderUrl = JsonSerializer.Deserialize<string>(content);
            if (!string.IsNullOrEmpty(leaderUrl))
            {
              return leaderUrl;
            }
          }
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error finding leader from {url}");
        }
      }
      return null;
    }

    public async Task<int?> EventualGetAsync(string key)
    {
      var nodeUrl = nodeUrls[random.Next(nodeUrls.Count)];
      try
      {
        var response = await httpClient.GetAsync($"{nodeUrl}/api/raftnode/eventualget?key={key}");
        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          return JsonSerializer.Deserialize<int?>(content);
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Error performing EventualGet for key {key} on {nodeUrl}");
      }
      return null;
    }

    public async Task<int?> StrongGetAsync(string key)
    {
      var leaderUrl = await FindLeaderAsync();
      if (leaderUrl != null)
      {
        try
        {
          var response = await httpClient.GetAsync($"{leaderUrl}/api/raftnode/strongget?key={key}");
          if (response.IsSuccessStatusCode)
          {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<int?>(content);
          }
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error performing StrongGet for key {key} on leader {leaderUrl}");
        }
      }
      return null;
    }

    public async Task<bool> CompareVersionAndSwapAsync(string key, int expectedValue, int newValue)
    {
      var leaderUrl = await FindLeaderAsync();
      if (leaderUrl != null)
      {
        try
        {
          var content = new FormUrlEncodedContent(new[]
          {
              new KeyValuePair<string, string>("key", key),
              new KeyValuePair<string, string>("expectedValue", expectedValue.ToString()),
              new KeyValuePair<string, string>("newValue", newValue.ToString())
          });

          var response = await httpClient.PostAsync($"{leaderUrl}/api/raftnode/compareversionandswap", content);
          return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error performing CompareVersionAndSwap for key {key} on leader {leaderUrl}");
        }
      }
      return false;
    }

    public async Task<bool> WriteAsync(string key, int value)
    {
      var leaderUrl = await FindLeaderAsync();
      if (leaderUrl != null)
      {
        try
        {
          var content = new FormUrlEncodedContent(new[]
          {
              new KeyValuePair<string, string>("key", key),
              new KeyValuePair<string, string>("value", value.ToString()),
          });

          var response = await httpClient.PostAsync($"{leaderUrl}/api/raftnode/write", content);
          return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error performing Write for key {key} on leader {leaderUrl}");
        }
      }
      return false;
    }
  }
}
