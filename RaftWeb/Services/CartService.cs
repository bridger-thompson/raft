using System.Text.Json;
using RaftShared;
using RaftWeb.Models;

namespace RaftWeb.Services;

public class CartService(RaftService service)
{
  private readonly string cartKey = "cart-of-";
  private readonly RaftService service = service;

  public string GetKey(string username)
  {
    return cartKey + username.Replace(' ', '-');
  }

  public async Task AddToCart(Cart cart, Product newItem, Data lastCartData)
  {
    var existingProduct = cart.Items.FirstOrDefault(p => p.Name == newItem.Name);
    if (existingProduct != null)
    {
      existingProduct.Quantity += 1;
    }
    else
    {
      newItem.Quantity = 1;
      cart.Items.Add(newItem);
    }
    var newCart = CartToJson(cart);
    await service.TryUpdate(GetKey(cart.Username), lastCartData.Value, newCart, lastCartData.LogIndex);
  }


  public Cart JsonToCart(string jsonString)
  {
    return JsonSerializer.Deserialize<Cart>(jsonString) ?? new Cart();
  }

  public string CartToJson(Cart cart)
  {
    return JsonSerializer.Serialize(cart, new JsonSerializerOptions { WriteIndented = true });
  }

}