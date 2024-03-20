using System.Text.Json;
using RaftShared;
using RaftWeb.Models;

namespace RaftWeb.Services;

public class OrderService(RaftService service)
{
  private readonly string orderKey = "order-info-";
  private readonly string orderStatusKey = "order-status-";
  private readonly string pendingOrdersKey = "pending-orders";
  private readonly RaftService service = service;

  public string GetKey(Guid id)
  {
    return orderKey + id;
  }

  public string GetStatusKey(Guid id)
  {
    return orderStatusKey + id;
  }

  public async Task<Guid[]> GetPendingOrders()
  {
    var result = await service.StrongGet(pendingOrdersKey);
    Guid[] pendingOrders = [];
    if (result.LogIndex != -1)
    {
      pendingOrders = JsonSerializer.Deserialize<Guid[]>(result.Value) ?? [];
    }
    return pendingOrders;
  }

  public async Task<(Cart?, string)> GetOrder(Guid id)
  {
    var cartResult = await service.StrongGet(GetKey(id));
    var cart = JsonSerializer.Deserialize<Cart>(cartResult.Value);
    var statusResult = await service.StrongGet(GetStatusKey(id));
    return (cart, statusResult.Value);
  }

  public async Task AddOrder(Cart cart)
  {
    Guid id = Guid.NewGuid();
    // add order
    var newOrder = JsonSerializer.Serialize(cart);
    await service.TryUpdate(GetKey(id), "None", newOrder, -1);

    // add order status
    await service.TryUpdate(GetStatusKey(id), "None", "pending", -1);

    // add to pending orders
    var result = await service.StrongGet(pendingOrdersKey);
    var pendingOrders = await GetPendingOrders();
    pendingOrders = [.. pendingOrders, id];
    var newPendingOrders = JsonSerializer.Serialize(pendingOrders);
    await service.TryUpdate(pendingOrdersKey, result.Value, newPendingOrders, result.LogIndex);
  }

}