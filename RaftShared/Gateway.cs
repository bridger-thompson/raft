using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text;
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
          var response = await httpClient.GetAsync($"{url}/RaftNode/leader");
          Console.WriteLine($"Response from {url}: {response.StatusCode}");
          if (response.IsSuccessStatusCode)
          {
            var content = await response.Content.ReadAsStringAsync();
            var isLeader = bool.Parse(content);
            Console.WriteLine($"Is Leader: {isLeader}");
            if (isLeader)
            {
              return url;
            }
          }
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error finding leader from {url}");
        }
      }
      logger.LogError($"Error finding leader");
      return null;
    }

    public async Task<Data?> EventualGetAsync(string key)
    {
      var nodeUrl = nodeUrls[random.Next(nodeUrls.Count)];
      try
      {
        var response = await httpClient.GetAsync($"{nodeUrl}/RaftNode/eventualget?key={key}");
        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          var options = new JsonSerializerOptions
          {
            PropertyNameCaseInsensitive = true
          };
          var getResult = JsonSerializer.Deserialize<Data>(content, options);
          return getResult;
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Error performing EventualGet for key {key} on {nodeUrl}");
      }
      return null;
    }

    public async Task<Data?> StrongGetAsync(string key)
    {
      var leaderUrl = await FindLeaderAsync();
      if (leaderUrl != null)
      {
        try
        {
          var response = await httpClient.GetAsync($"{leaderUrl}/RaftNode/strongget?key={key}");
          if (response.IsSuccessStatusCode)
          {
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
              PropertyNameCaseInsensitive = true
            };
            var getResult = JsonSerializer.Deserialize<Data>(content, options);
            Console.WriteLine($"SG Results: {content} {getResult.Value}, {getResult.LogIndex}");
            if (getResult != null)
            {
              return getResult;
            }
          }
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error performing StrongGet for key {key} on leader {leaderUrl}");
        }
      }
      return null;
    }

    public async Task<bool> CompareVersionAndSwapAsync(string key, string expectedValue, string newValue, int expectedLogIndex)
    {
      var leaderUrl = await FindLeaderAsync();
      if (leaderUrl != null)
      {
        try
        {
          var content = new FormUrlEncodedContent(new[]
          {
              new KeyValuePair<string, string>("key", key),
              new KeyValuePair<string, string>("expectedValue", expectedValue),
              new KeyValuePair<string, string>("newValue", newValue),
              new KeyValuePair<string, string>("expectedLogIndex", expectedLogIndex.ToString())
          });

          var response = await httpClient.PostAsync($"{leaderUrl}/RaftNode/CompareVersionAndSwap", content);
          return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error performing CompareVersionAndSwap for key {key} on leader {leaderUrl}");
        }
      }
      return false;
    }

    public async Task<bool> WriteAsync(string key, string value)
    {
      var leaderUrl = await FindLeaderAsync();
      if (leaderUrl != null)
      {
        try
        {
          var payload = new WriteModel { Key = key, Value = value };
          var json = JsonSerializer.Serialize(payload);
          var content = new StringContent(json, Encoding.UTF8, "application/json");

          var response = await httpClient.PostAsync($"{leaderUrl}/RaftNode/Write", content);
          return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error performing Write for key {key} on leader {leaderUrl}");
        }
      }
      logger.LogError($"Error performing Write for key {key}. No leader found.");
      return false;
    }
  }
}
