namespace RaftShared;

public class Product
{
  public string Name { get; set; } = string.Empty;
  public decimal Cost { get; set; }
  public Data? QuantityData { get; set; }
}
