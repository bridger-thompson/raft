

using RaftShared;

namespace RaftWeb.Services;

public class BalanceService(BalanceConverter converter, RaftService service)
{
  private readonly string balanceKey = "balance-of-";
  private readonly BalanceConverter converter = converter;
  private readonly RaftService service = service;

  public string GetKey(string username)
  {
    return balanceKey + username.Replace(' ', '-');
  }

  public decimal? GetBalance(Data result)
  {
    decimal? balance = null;
    if (result.LogIndex != -1)
    {
      balance = converter.JsonToBalance(result.Value);
    }
    return balance;
  }

  public async Task Deposit(decimal? balance, decimal amount, string username, Data lastData)
  {
    var newBalance = converter.BalanceToJson((balance ?? 0) + amount);
    await service.TryUpdate(GetKey(username), lastData.Value, newBalance, lastData.LogIndex);
  }
}