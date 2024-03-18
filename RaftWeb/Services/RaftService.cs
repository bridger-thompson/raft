using System.Net.Http.Json;
using RaftShared;

namespace RaftWeb.Services;


public class RaftService(HttpClient httpClient)
{
  private readonly HttpClient _httpClient = httpClient;

  public async Task<Data> StrongGet(string key)
  {
    var results = await _httpClient.GetFromJsonAsync<Data>($"/RaftGateway/StrongGet?key={key}");
    return results is not null ? results : new Data() { Value = "", LogIndex = -1 };
  }

  public async Task TryUpdate(string key, string expectedValue, string newValue, int expectedLogIndex)
  {
    var response = await _httpClient.PostAsync($"/RaftGateway/CompareVersionAndSwap?key={key}&expectedValue={expectedValue}&newValue={newValue}&expectedLogIndex={expectedLogIndex}", null);
  }
}