namespace RaftWeb.Models;

public class Cart
{
  public string Username { get; set; } = string.Empty;
  public List<Product> Items { get; set; } = [];
}