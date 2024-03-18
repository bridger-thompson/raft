using System.Text.Json;

namespace RaftWeb.Services;

public class QuantityConverter
{
  public string QuantityToJson(int quantity)
  {
    var quantityObject = new { quantity = quantity };
    return JsonSerializer.Serialize(quantityObject);
  }

  public int JsonToQuantity(string jsonString)
  {
    var quantityObject = JsonSerializer.Deserialize<QuantityObject>(jsonString);
    return quantityObject?.quantity ?? -1; // Default to -1 if null
  }

  private class QuantityObject
  {
    public int quantity { get; set; }
  }
}
