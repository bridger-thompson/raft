using System.Net.Http.Json;
using RaftShared;

namespace RaftWeb.Services;


public class RaftService(HttpClient httpClient)
{
  private readonly HttpClient _httpClient = httpClient;

  public async Task<Data> StrongGet(string key)
  {
    var results = await _httpClient.GetFromJsonAsync<Data>($"/RaftGateway?key={key}");
    return results is not null ? results : new Data() { Value = -1, LogIndex = -1 };
  }
}