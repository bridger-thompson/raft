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
    await UpdateOrderStatus(id, "pending");

    // add to pending orders (keep retrying until a timeout)
    TimeSpan timeout = TimeSpan.FromSeconds(30);
    DateTime startTime = DateTime.UtcNow;
    bool updateSuccessful = false;

    while (DateTime.UtcNow - startTime < timeout && !updateSuccessful)
    {
      try
      {
        await AddPendingOrder(id);

        updateSuccessful = true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error adding pending order: {ex}");
        await Task.Delay(1000);
      }
    }

    if (!updateSuccessful)
    {
      throw new TimeoutException("Failed to add order to pending orders within the timeout period.");
    }
  }

  public async Task AddPendingOrder(Guid id)
  {
    var result = await service.StrongGet(pendingOrdersKey);
    var pendingOrders = await GetPendingOrders();
    pendingOrders = [.. pendingOrders, id];
    var newPendingOrders = JsonSerializer.Serialize(pendingOrders);
    await service.TryUpdate(pendingOrdersKey, result.Value, newPendingOrders, result.LogIndex);
  }

  public async Task RemovePendingOrder(Guid id)
  {
    TimeSpan timeout = TimeSpan.FromSeconds(30);
    DateTime startTime = DateTime.UtcNow;
    bool updateSuccessful = false;

    while (DateTime.UtcNow - startTime < timeout && !updateSuccessful)
    {
      try
      {
        var result = await service.StrongGet(pendingOrdersKey);
        var pendingOrders = await GetPendingOrders();
        var remainingOrders = JsonSerializer.Serialize(pendingOrders.Where(p => p != id));
        await service.TryUpdate(pendingOrdersKey, result.Value, remainingOrders, result.LogIndex);

        updateSuccessful = true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error adding pending order: {ex}");
        await Task.Delay(1000);
      }
    }

    if (!updateSuccessful)
    {
      throw new TimeoutException("Failed to add order to pending orders within the timeout period.");
    }

  }

  public async Task UpdateOrderStatus(Guid id, string status)
  {
    var statusResult = await GetOrderStatus(id);
    await service.TryUpdate(GetStatusKey(id), statusResult.Value, status, statusResult.LogIndex);
  }

  public async Task<Data> GetOrderStatus(Guid id)
  {
    return await service.StrongGet(GetStatusKey(id));
  }

}