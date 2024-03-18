using System.Text.Json;

namespace RaftWeb.Services;

public class BalanceConverter
{
  public string BalanceToJson(decimal balance)
  {
    var balanceObject = new { balance = balance };
    return JsonSerializer.Serialize(balanceObject);
  }

  public decimal JsonToBalance(string jsonString)
  {
    var balanceObject = JsonSerializer.Deserialize<BalanceObject>(jsonString);
    return balanceObject?.balance ?? -1;
  }

  private class BalanceObject
  {
    public decimal balance { get; set; }
  }
}
