namespace FinalProject.Models;

/// <summary>
/// Represents a purchasable item placed inside the room with a fixed position.
/// </summary>
public class RoomItem
{
    /// <summary>Shop item id used to resolve metadata.</summary>
    public string ItemId { get; set; } = "";
    /// <summary>Asset path for the rendered item.</summary>
    public string Asset { get; set; } = "";
    /// <summary>Normalized X coordinate for layout placement.</summary>
    public double X { get; set; }
    /// <summary>Normalized Y coordinate for layout placement.</summary>
    public double Y { get; set; }
    /// <summary>Optional scale multiplier for rendering size.</summary>
    public double Scale { get; set; } = 1;
}