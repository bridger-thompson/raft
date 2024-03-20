using RaftWeb.Models;
using RaftShared;

namespace RaftWeb.Services;

class ProductService(RaftService service, QuantityConverter converter)
{
  private readonly RaftService service = service;
  private readonly QuantityConverter converter = converter;
  private string stockKey = "stock-of-";

  public List<Product> GetProducts()
  {
    var products = new List<Product>
      {
        new() { Name = "Product 1", Cost = 1.00m },
        new() { Name = "Product 2", Cost = 1.00m },
        new() { Name = "Product 3", Cost = 1.00m },
        new() { Name = "Product 4", Cost = 1.00m },
        new() { Name = "Product 5", Cost = 1.00m }
      };
    return products;
  }

  public string GetKey(Product product)
  {
    return stockKey + product.Name.Replace(' ', '-');
  }

  public int? LoadQuantity(Data result)
  {
    int? quantity = null;
    if (result.LogIndex != -1)
    {
      quantity = converter.JsonToQuantity(result.Value);
    }
    return quantity;
  }

  public async Task IncreaseQuantity(string key, int? quantity, Data lastData)
  {
    var newQuantity = converter.QuantityToJson((quantity ?? 0) + 1);
    await service.TryUpdate(key, lastData.Value, newQuantity, lastData.LogIndex);
  }

  public async Task DecreaseQuantity(string key, int? quantity, Data lastData)
  {
    var newQuantity = converter.QuantityToJson((quantity ?? 0) - 1);
    await service.TryUpdate(key, lastData.Value, newQuantity, lastData.LogIndex);
  }
}