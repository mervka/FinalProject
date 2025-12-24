namespace FinalProject.Models;

public class ShopItem
{
    public string Name { get; set; } = "";
    public int Price { get; set; }
    public string Icon { get; set; } = "";
    public ItemType Type { get; set; }
}

public enum ItemType
{
    Food,
    Toy,
    Furniture,
    Health
}