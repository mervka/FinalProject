using System;
namespace FinalProject.Models;


public class ShopItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Price { get; set; }
    public string Icon { get; set; } = "";
    public ItemType Type { get; set; }
    //public bool PlaceInRoom { get; set; }
    public int HungerEffect { get; set; }
    public int HappinessEffect { get; set; }
    public int HealthEffect { get; set; }
    public string CategoryId { get; set; }
}

public enum ItemType
{
    Food,
    Toy,
    Furniture,
    Health
}