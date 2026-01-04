using System;
namespace FinalProject.Models;

/// <summary>
/// Defines a purchasable item and the stat effects it applies.
/// </summary>
public class ShopItem
{
    /// <summary>Stable identifier for saving ownership.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Price { get; set; }
    public string Icon { get; set; } = "";
    public ItemType Type { get; set; }
    /// <summary>If true, the item is placed in the room after purchase.</summary>
    public bool PlaceInRoom { get; set; }
    /// <summary>Stat effects applied on purchase.</summary>
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