using System.Text.Json;

namespace RaftWeb.Services;

public class QuantityConverter
{
  public string QuantityToJson(int quantity)
  {
    var quantityObject = new { quantity };
    return JsonSerializer.Serialize(quantityObject);
  }

  public int JsonToQuantity(string jsonString)
  {
    var options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };
    var quantityObject = JsonSerializer.Deserialize<QuantityObject>(jsonString, options);
    return quantityObject?.Quantity ?? -1;
  }

  private class QuantityObject
  {
    public int Quantity { get; set; }
  }
}
